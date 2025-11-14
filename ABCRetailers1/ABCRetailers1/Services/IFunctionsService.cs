using ABCRetailers.Functions.Models;

namespace ABCRetailers.Services
{
    public interface IFunctionsService
    {
        // Customer methods
        Task<ApiResponse<List<CustomerApiModel>>> GetCustomersAsync();
        Task<ApiResponse<CustomerApiModel>> GetCustomerAsync(string id);
        Task<ApiResponse<CustomerApiModel>> CreateCustomerAsync(CustomerApiModel customer);
        Task<ApiResponse<CustomerApiModel>> UpdateCustomerAsync(string id, CustomerApiModel customer);
        Task<ApiResponse<object>> DeleteCustomerAsync(string id);

        // Product methods
        Task<ApiResponse<List<ProductApiModel>>> GetProductsAsync();
        Task<ApiResponse<ProductApiModel>> GetProductAsync(string id);
        Task<ApiResponse<ProductApiModel>> CreateProductAsync(ProductApiModel product);
        Task<ApiResponse<ProductApiModel>> UpdateProductAsync(string id, ProductApiModel product);
        Task<ApiResponse<object>> DeleteProductAsync(string id);

        // Order methods
        Task<ApiResponse<List<OrderApiModel>>> GetOrdersAsync();
        Task<ApiResponse<OrderApiModel>> GetOrderAsync(string id);
        Task<ApiResponse<OrderApiModel>> CreateOrderAsync(CreateOrderRequest order);
        Task<ApiResponse<OrderApiModel>> UpdateOrderStatusAsync(string id, string status);
        Task<ApiResponse<object>> DeleteOrderAsync(string id);
    }
}