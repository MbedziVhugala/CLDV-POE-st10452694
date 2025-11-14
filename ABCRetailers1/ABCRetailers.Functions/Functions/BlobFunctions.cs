using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Helpers;
using ABCRetailers.Functions.Services;
using ABCRetailers.Functions.Models;

using System.Text.Json;

namespace ABCRetailers.Functions.Functions
{
    public class BlobFunctions
    {
        private readonly ILogger<BlobFunctions> _logger;

        public BlobFunctions(ILogger<BlobFunctions> logger)
        {
            _logger = logger;
        }

        [Function("UploadFile")]
        public async Task<HttpResponseData> UploadFile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload")] HttpRequestData req)
        {
            try
            {
                _logger.LogInformation("File upload request received");

                // For now, just return success - we'll implement actual file upload later
                var response = new { success = true, message = "File upload endpoint ready" };
                return await HttpJsonHelper.CreateJsonResponseAsync(req, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in upload file function");
                return HttpJsonHelper.CreateErrorResponse(req, "Upload failed");
            }
        }

        [Function("GetBlobInfo")]
        public async Task<HttpResponseData> GetBlobInfo(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "blobs/{container}")] HttpRequestData req,
            string container)
        {
            try
            {
                _logger.LogInformation("Getting blob info for container: {Container}", container);

                var response = new
                {
                    success = true,
                    container = container,
                    message = "Blob storage endpoint ready"
                };
                return await HttpJsonHelper.CreateJsonResponseAsync(req, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting blob info");
                return HttpJsonHelper.CreateErrorResponse(req, "Failed to get blob info");
            }
        }
    }
}