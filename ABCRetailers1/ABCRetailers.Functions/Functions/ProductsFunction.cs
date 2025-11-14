using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Helpers;
using ABCRetailers.Functions.Models;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Services;

namespace ABCRetailers.Functions.Functions
{
    public class ProductsFunctions
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductsFunctions> _logger;

        public ProductsFunctions(IAzureStorageService storageService, ILogger<ProductsFunctions> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [Function("GetProducts")]
        public async Task<HttpResponseData> GetProducts(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting all products");
                var entities = await _storageService.GetAllEntitiesAsync<ProductEntity>();
                var products = entities.Select(e => e.ToApiModel()).ToList();

                var response = ApiResponse<List<ProductApiModel>>.SuccessResponse(products, "Products retrieved successfully");
                return await HttpJsonHelper.CreateJsonResponseAsync(req, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return HttpJsonHelper.CreateErrorResponse(req, "Failed to retrieve products");
            }
        }

        [Function("GetProduct")]
        public async Task<HttpResponseData> GetProduct(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "products/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("Getting product with ID: {ProductId}", id);
                var product = await _storageService.GetEntityAsync<ProductEntity>("Product", id);

                if (product == null)
                    return HttpJsonHelper.CreateErrorResponse(req, "Product not found", System.Net.HttpStatusCode.NotFound);

                var response = ApiResponse<ProductApiModel>.SuccessResponse(product.ToApiModel(), "Product retrieved successfully");
                return await HttpJsonHelper.CreateJsonResponseAsync(req, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product with ID: {ProductId}", id);
                return HttpJsonHelper.CreateErrorResponse(req, "Failed to retrieve product");
            }
        }

        [Function("CreateProduct")]
        public async Task<HttpResponseData> CreateProduct(
      [HttpTrigger(AuthorizationLevel.Function, "post", Route = "products")] HttpRequestData req)
        {
            try
            {
                var productRequest = await HttpJsonHelper.ReadRequestAsync<ProductApiModel>(req);
                if (productRequest == null)
                    return HttpJsonHelper.CreateErrorResponse(req, "Invalid product data");

                var productEntity = new ProductEntity
                {
                    RowKey = Guid.NewGuid().ToString(),
                    ProductName = productRequest.ProductName,
                    Description = productRequest.Description,
                    StockAvailable = productRequest.StockAvailable,
                    ImageUrl = productRequest.ImageUrl
                };

                // IMPORTANT: Set the PriceString directly
                productEntity.PriceString = productRequest.Price.ToString("F2");

                // Debug: Check what's being stored
                Console.WriteLine($"Creating product - Price: {productRequest.Price}, PriceString: {productEntity.PriceString}");

                var createdProduct = await _storageService.AddEntityAsync(productEntity);

                _logger.LogInformation("Created product with ID: {ProductId}, Price: {Price}",
                    createdProduct.RowKey, createdProduct.Price);

                await _storageService.SendMessageAsync("stock-updates",
                    $"New product: {createdProduct.ProductName} with stock: {createdProduct.StockAvailable}");

                var response = ApiResponse<ProductApiModel>.SuccessResponse(createdProduct.ToApiModel(), "Product created successfully");
                return await HttpJsonHelper.CreateJsonResponseAsync(req, response, System.Net.HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return HttpJsonHelper.CreateErrorResponse(req, "Failed to create product");
            }
        }
    }
}