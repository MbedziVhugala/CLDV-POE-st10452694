using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Helpers;
using ABCRetailers.Functions.Models;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Services;

namespace ABCRetailers.Functions.Functions
{
    public class OrdersFunctions
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<OrdersFunctions> _logger;

        public OrdersFunctions(IAzureStorageService storageService, ILogger<OrdersFunctions> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [Function("GetOrders")]
        public async Task<HttpResponseData> GetOrders(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting all orders");
                var entities = await _storageService.GetAllEntitiesAsync<OrderEntity>();
                var orders = entities.Select(e => e.ToApiModel()).ToList();

                var response = ApiResponse<List<OrderApiModel>>.SuccessResponse(orders, "Orders retrieved successfully");
                return await HttpJsonHelper.CreateJsonResponseAsync(req, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                return HttpJsonHelper.CreateErrorResponse(req, "Failed to retrieve orders");
            }
        }

        [Function("CreateOrder")]
        public async Task<HttpResponseData> CreateOrder(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
        {
            try
            {
                var orderRequest = await HttpJsonHelper.ReadRequestAsync<CreateOrderRequest>(req);
                if (orderRequest == null)
                    return HttpJsonHelper.CreateErrorResponse(req, "Invalid order data");

                var customer = await _storageService.GetEntityAsync<CustomerEntity>("Customer", orderRequest.CustomerId);
                var product = await _storageService.GetEntityAsync<ProductEntity>("Product", orderRequest.ProductId);

                if (customer == null || product == null)
                    return HttpJsonHelper.CreateErrorResponse(req, "Customer or product not found");

                if (product.StockAvailable < orderRequest.Quantity)
                    return HttpJsonHelper.CreateErrorResponse(req, "Insufficient stock available");

                // DEBUG: Check the product price
                Console.WriteLine($"Product PriceString: {product.PriceString}");
                Console.WriteLine($"Product Price (calculated): {product.Price}");

                // Make sure we're using the correct price
                var unitPrice = product.Price; // This should use the calculated Price property
                var totalPrice = unitPrice * orderRequest.Quantity;

                var orderEntity = new OrderEntity
                {
                    RowKey = Guid.NewGuid().ToString(),
                    CustomerId = orderRequest.CustomerId,
                    Username = customer.Username,
                    ProductId = orderRequest.ProductId,
                    ProductName = product.ProductName,
                    OrderDate = DateTime.UtcNow.Date,
                    Quantity = orderRequest.Quantity,
                    UnitPrice = unitPrice,  // Use the variable
                    TotalPrice = totalPrice, // Use the calculated total
                    Status = "Submitted"
                };

                var createdOrder = await _storageService.AddEntityAsync(orderEntity);

                // Update stock
                product.StockAvailable -= orderRequest.Quantity;
                await _storageService.UpdateEntityAsync(product);

                _logger.LogInformation("Created order with ID: {OrderId}, UnitPrice: {UnitPrice}, TotalPrice: {TotalPrice}",
                    createdOrder.RowKey, unitPrice, totalPrice);

                // Queue message for order notification
                await _storageService.SendMessageAsync("order-notifications",
                    $"New order: {createdOrder.RowKey} for {customer.Username} - Total: {totalPrice:C}");

                var response = ApiResponse<OrderApiModel>.SuccessResponse(createdOrder.ToApiModel(), "Order created successfully");
                return await HttpJsonHelper.CreateJsonResponseAsync(req, response, System.Net.HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return HttpJsonHelper.CreateErrorResponse(req, "Failed to create order");
            }
        }
    }
}