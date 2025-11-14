using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;

namespace ABCRetailers.Functions.Helpers
{
    public static class HttpJsonHelper
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public static async Task<T?> ReadRequestAsync<T>(HttpRequestData req)
        {
            try
            {
                req.Body.Position = 0;
                using var reader = new StreamReader(req.Body);
                var json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading request: {ex.Message}");
                return default;
            }
        }

        public static async Task<HttpResponseData> CreateJsonResponseAsync<T>(
            HttpRequestData req,
            T data,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            await response.WriteStringAsync(json);
            return response;
        }

        public static HttpResponseData CreateErrorResponse(
            HttpRequestData req,
            string message,
            System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.BadRequest)
        {
            var errorResponse = new { success = false, message };
            return CreateJsonResponseAsync(req, errorResponse, statusCode).Result;
        }
    }
}