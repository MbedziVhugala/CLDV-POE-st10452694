using System.Text;
using System.Text.Json;
using ABCRetailers.Functions.Models;
using ABCRetailers.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ABCRetailers.Services
{
    public class FunctionsService : IFunctionsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FunctionsService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public FunctionsService(IConfiguration configuration, ILogger<FunctionsService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _httpClient = new HttpClient();
            var functionsBaseUrl = _configuration["FunctionsBaseUrl"] ?? "http://localhost:7071/api/";
            _httpClient.BaseAddress = new Uri(functionsBaseUrl);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        // Customer methods
        public async Task<ApiResponse<List<CustomerApiModel>>> GetCustomersAsync()
        {
            return await SendRequestAsync<ApiResponse<List<CustomerApiModel>>>("customers", HttpMethod.Get);
        }

        public async Task<ApiResponse<CustomerApiModel>> GetCustomerAsync(string id)
        {
            return await SendRequestAsync<ApiResponse<CustomerApiModel>>($"customers/{id}", HttpMethod.Get);
        }

        public async Task<ApiResponse<CustomerApiModel>> CreateCustomerAsync(CustomerApiModel customer)
        {
            return await SendRequestAsync<ApiResponse<CustomerApiModel>>("customers", HttpMethod.Post, customer);
        }

        public async Task<ApiResponse<CustomerApiModel>> UpdateCustomerAsync(string id, CustomerApiModel customer)
        {
            return await SendRequestAsync<ApiResponse<CustomerApiModel>>($"customers/{id}", HttpMethod.Put, customer);
        }

        public async Task<ApiResponse<object>> DeleteCustomerAsync(string id)
        {
            return await SendRequestAsync<ApiResponse<object>>($"customers/{id}", HttpMethod.Delete);
        }

        // Product methods
        public async Task<ApiResponse<List<ProductApiModel>>> GetProductsAsync()
        {
            return await SendRequestAsync<ApiResponse<List<ProductApiModel>>>("products", HttpMethod.Get);
        }

        public async Task<ApiResponse<ProductApiModel>> GetProductAsync(string id)
        {
            return await SendRequestAsync<ApiResponse<ProductApiModel>>($"products/{id}", HttpMethod.Get);
        }

        public async Task<ApiResponse<ProductApiModel>> CreateProductAsync(ProductApiModel product)
        {
            return await SendRequestAsync<ApiResponse<ProductApiModel>>("products", HttpMethod.Post, product);
        }

        public async Task<ApiResponse<ProductApiModel>> UpdateProductAsync(string id, ProductApiModel product)
        {
            return await SendRequestAsync<ApiResponse<ProductApiModel>>($"products/{id}", HttpMethod.Put, product);
        }

        public async Task<ApiResponse<object>> DeleteProductAsync(string id)
        {
            return await SendRequestAsync<ApiResponse<object>>($"products/{id}", HttpMethod.Delete);
        }

        // Order methods
        public async Task<ApiResponse<List<OrderApiModel>>> GetOrdersAsync()
        {
            return await SendRequestAsync<ApiResponse<List<OrderApiModel>>>("orders", HttpMethod.Get);
        }

        public async Task<ApiResponse<OrderApiModel>> GetOrderAsync(string id)
        {
            return await SendRequestAsync<ApiResponse<OrderApiModel>>($"orders/{id}", HttpMethod.Get);
        }

        public async Task<ApiResponse<OrderApiModel>> CreateOrderAsync(CreateOrderRequest order)
        {
            return await SendRequestAsync<ApiResponse<OrderApiModel>>("orders", HttpMethod.Post, order);
        }

        public async Task<ApiResponse<OrderApiModel>> UpdateOrderStatusAsync(string id, string status)
        {
            var request = new UpdateOrderStatusRequest { Status = status };
            return await SendRequestAsync<ApiResponse<OrderApiModel>>($"orders/{id}/status", HttpMethod.Patch, request);
        }

        public async Task<ApiResponse<object>> DeleteOrderAsync(string id)
        {
            return await SendRequestAsync<ApiResponse<object>>($"orders/{id}", HttpMethod.Delete);
        }

        private async Task<T> SendRequestAsync<T>(string endpoint, HttpMethod method, object? content = null) where T : class
        {
            try
            {
                var request = new HttpRequestMessage(method, endpoint);

                if (content != null)
                {
                    var json = JsonSerializer.Serialize(content, _jsonOptions);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
                    if (result != null)
                    {
                        return result;
                    }
                }

                _logger.LogError("HTTP {Method} to {Endpoint} failed with status {StatusCode}: {Response}",
                    method, endpoint, response.StatusCode, responseContent);

                // Return a default error response
                return CreateErrorResponse<T>($"Request failed with status {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Functions endpoint: {Endpoint}", endpoint);
                return CreateErrorResponse<T>($"Request failed: {ex.Message}");
            }
        }

        private static T CreateErrorResponse<T>(string message) where T : class
        {
            // Handle all possible return types from IFunctionsService using YOUR existing ApiModels
            if (typeof(T) == typeof(ApiResponse<List<CustomerApiModel>>))
            {
                return (ApiResponse<List<CustomerApiModel>>.ErrorResponse(message) as T)!;
            }
            else if (typeof(T) == typeof(ApiResponse<CustomerApiModel>))
            {
                return (ApiResponse<CustomerApiModel>.ErrorResponse(message) as T)!;
            }
            else if (typeof(T) == typeof(ApiResponse<object>))
            {
                return (ApiResponse<object>.ErrorResponse(message) as T)!;
            }
            else if (typeof(T) == typeof(ApiResponse<List<ProductApiModel>>))
            {
                return (ApiResponse<List<ProductApiModel>>.ErrorResponse(message) as T)!;
            }
            else if (typeof(T) == typeof(ApiResponse<ProductApiModel>))
            {
                return (ApiResponse<ProductApiModel>.ErrorResponse(message) as T)!;
            }
            else if (typeof(T) == typeof(ApiResponse<List<OrderApiModel>>))
            {
                return (ApiResponse<List<OrderApiModel>>.ErrorResponse(message) as T)!;
            }
            else if (typeof(T) == typeof(ApiResponse<OrderApiModel>))
            {
                return (ApiResponse<OrderApiModel>.ErrorResponse(message) as T)!;
            }

            // Fallback for any other types
            return Activator.CreateInstance<T>();
        }


          
        }
    }
