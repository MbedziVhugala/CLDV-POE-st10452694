using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Helpers;
using ABCRetailers.Functions.Models;
using ABCRetailers.Services;
using System.Text.Json;

namespace ABCRetailers.Functions.Functions
{
    public class UploadFunctions
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<UploadFunctions> _logger;

        public UploadFunctions(IAzureStorageService storageService, ILogger<UploadFunctions> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        [Function("UploadPaymentProof")]
        public async Task<HttpResponseData> UploadPaymentProof(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload/payment-proof")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("Processing payment proof upload");

                // Use JSON-based approach instead of multipart for simplicity in Functions
                var request = await HttpJsonHelper.ReadRequestAsync<PaymentProofRequest>(req);
                if (request == null || string.IsNullOrEmpty(request.FileName))
                {
                    return await HttpJsonHelper.CreateJsonResponseAsync(req,
                        ApiResponse<object>.ErrorResponse("Invalid payment proof data"),
                        System.Net.HttpStatusCode.BadRequest);
                }

                _logger.LogInformation("Payment proof upload for order: {OrderId}", request.OrderId);

                // In a real implementation, you would:
                // 1. Decode base64 data if provided
                // 2. Upload to blob storage
                // 3. Update order status

                return await HttpJsonHelper.CreateJsonResponseAsync(req,
                    ApiResponse<object>.SuccessResponse(new { FileName = request.FileName },
                    "Payment proof upload processed"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment proof upload");
                return await HttpJsonHelper.CreateJsonResponseAsync(req,
                    ApiResponse<object>.ErrorResponse($"Upload failed: {ex.Message}"),
                    System.Net.HttpStatusCode.InternalServerError);
            }
        }

        [Function("GetUploadStatus")]
        public async Task<HttpResponseData> GetUploadStatus(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "upload/status/{orderId}")] HttpRequestData req,
            string orderId)
        {
            try
            {
                _logger.LogInformation("Getting upload status for order: {OrderId}", orderId);

                // Return mock status - implement actual status checking
                var status = new
                {
                    OrderId = orderId,
                    HasPaymentProof = false,
                    LastUpdated = DateTime.UtcNow
                };

                return await HttpJsonHelper.CreateJsonResponseAsync(req,
                    ApiResponse<object>.SuccessResponse(status, "Upload status retrieved"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upload status for order: {OrderId}", orderId);
                return await HttpJsonHelper.CreateJsonResponseAsync(req,
                    ApiResponse<object>.ErrorResponse($"Failed to get status: {ex.Message}"),
                    System.Net.HttpStatusCode.InternalServerError);
            }
        }
    }

    public class PaymentProofRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Base64Data { get; set; } = string.Empty;
    }
}