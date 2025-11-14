using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Helpers;
using ABCRetailers.Functions.Models;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Services;

namespace ABCRetailers.Functions.Functions
{
    public class CustomersFunctions
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<CustomersFunctions> _logger;

        public CustomersFunctions(IAzureStorageService storageService, ILogger<CustomersFunctions> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [Function("GetCustomers")]
        public async Task<HttpResponseData> GetCustomers(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Getting all customers");
                var entities = await _storageService.GetAllEntitiesAsync<CustomerEntity>();
                var customers = entities.Select(e => e.ToApiModel()).ToList();

                var response = ApiResponse<List<CustomerApiModel>>.SuccessResponse(customers, "Customers retrieved successfully");
                return await HttpJsonHelper.CreateJsonResponseAsync(req, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers");
                return HttpJsonHelper.CreateErrorResponse(req, "Failed to retrieve customers");
            }
        }

        [Function("GetCustomer")]
        public async Task<HttpResponseData> GetCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{id}")] HttpRequestData req,
            string id)
        {
            try
            {
                _logger.LogInformation("Getting customer with ID: {CustomerId}", id);
                var customer = await _storageService.GetEntityAsync<CustomerEntity>("Customer", id);

                if (customer == null)
                    return HttpJsonHelper.CreateErrorResponse(req, "Customer not found", System.Net.HttpStatusCode.NotFound);

                var response = ApiResponse<CustomerApiModel>.SuccessResponse(customer.ToApiModel(), "Customer retrieved successfully");
                return await HttpJsonHelper.CreateJsonResponseAsync(req, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer with ID: {CustomerId}", id);
                return HttpJsonHelper.CreateErrorResponse(req, "Failed to retrieve customer");
            }
        }

        [Function("CreateCustomer")]
        public async Task<HttpResponseData> CreateCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers")] HttpRequestData req)
        {
            try
            {
                var customerRequest = await HttpJsonHelper.ReadRequestAsync<CustomerApiModel>(req);
                if (customerRequest == null)
                    return HttpJsonHelper.CreateErrorResponse(req, "Invalid customer data");

                var customerEntity = customerRequest.ToEntity();
                customerEntity.RowKey = Guid.NewGuid().ToString();
                var createdCustomer = await _storageService.AddEntityAsync(customerEntity);

                _logger.LogInformation("Created customer with ID: {CustomerId}", createdCustomer.RowKey);

                var response = ApiResponse<CustomerApiModel>.SuccessResponse(createdCustomer.ToApiModel(), "Customer created successfully");
                return await HttpJsonHelper.CreateJsonResponseAsync(req, response, System.Net.HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return HttpJsonHelper.CreateErrorResponse(req, "Failed to create customer");
            }
        }
    }
}