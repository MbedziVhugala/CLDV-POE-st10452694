using Azure.Data.Tables;

namespace ABCRetailers.Functions.Services
{
    public interface IAzureStorageService
    {
        Task<List<T>> GetAllEntitiesAsync<T>() where T : class, ITableEntity, new();
        Task<T?> GetEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();
        Task<T> AddEntityAsync<T>(T entity) where T : class, ITableEntity;
        Task<T> UpdateEntityAsync<T>(T entity) where T : class, ITableEntity;
        Task DeleteEntityAsync<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new();
        Task SendMessageAsync(string queueName, string message);
    }
}