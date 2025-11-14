using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Services;

namespace ABCRetailers.Functions.Functions
{
    public class QueueProcessorFunctions
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<QueueProcessorFunctions> _logger;

        public QueueProcessorFunctions(IAzureStorageService storageService, ILogger<QueueProcessorFunctions> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [Function("ProcessOrderNotifications")]
        public async Task ProcessOrderNotifications(
            [QueueTrigger("order-notifications", Connection = "AzureStorageConnection")] string queueMessage)
        {
            try
            {
                _logger.LogInformation("Processing order notification: {Message}", queueMessage);

                // Create audit entry in Orders table (REQUIREMENT: Queue trigger writes to storage table)
                var auditEntity = new OrderEntity
                {
                    PartitionKey = "Audit",
                    RowKey = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid()}",
                    ProductName = "System",
                    Status = "NotificationProcessed",
                    TotalPrice = 0,
                    Quantity = 0
                };

                await _storageService.AddEntityAsync(auditEntity);
                _logger.LogInformation("Order notification audit logged for: {Message}", queueMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order notification: {Message}", queueMessage);
                throw;
            }
        }

        [Function("ProcessStockUpdates")]
        public async Task ProcessStockUpdates(
            [QueueTrigger("stock-updates", Connection = "AzureStorageConnection")] string queueMessage)
        {
            try
            {
                _logger.LogInformation("Processing stock update: {Message}", queueMessage);
                // Additional stock processing logic can go here
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stock update: {Message}", queueMessage);
                throw;
            }
        }
    }
}