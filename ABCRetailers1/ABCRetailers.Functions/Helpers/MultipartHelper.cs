using Microsoft.Azure.Functions.Worker.Http;
using System.Text;

namespace ABCRetailers.Functions.Helpers
{
    public class MultipartHelper
    {
        public static async Task<Dictionary<string, string>> ParseMultipartFormData(HttpRequestData req)
        {
            var formData = new Dictionary<string, string>();

            try
            {
                // Reset stream position to beginning
                req.Body.Position = 0;

                // Read the entire request body
                using var reader = new StreamReader(req.Body, Encoding.UTF8);
                var bodyContent = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(bodyContent))
                {
                    return formData;
                }

                // Get boundary from Content-Type header
                if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
                {
                    throw new InvalidOperationException("Content-Type header is missing");
                }

                var contentType = contentTypeValues.First();
                var boundary = ExtractBoundary(contentType);

                if (string.IsNullOrEmpty(boundary))
                {
                    throw new InvalidOperationException("Could not extract boundary from Content-Type");
                }

                // Parse multipart form data
                return ParseMultipartBody(bodyContent, boundary);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse multipart form data", ex);
            }
        }

        private static Dictionary<string, string> ParseMultipartBody(string body, string boundary)
        {
            var formData = new Dictionary<string, string>();
            var boundaryMarker = $"--{boundary}";
            var endBoundary = $"{boundaryMarker}--";

            // Split into parts
            var parts = body.Split(new[] { boundaryMarker }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (part.Contains(endBoundary) || string.IsNullOrWhiteSpace(part))
                    continue;

                // Find field name and value
                var lines = part.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string fieldName = null;
                StringBuilder fieldValue = new StringBuilder();

                foreach (var line in lines)
                {
                    if (line.StartsWith("Content-Disposition:"))
                    {
                        // Extract field name
                        var nameIndex = line.IndexOf("name=\"");
                        if (nameIndex >= 0)
                        {
                            var endIndex = line.IndexOf("\"", nameIndex + 6);
                            if (endIndex >= 0)
                            {
                                fieldName = line.Substring(nameIndex + 6, endIndex - (nameIndex + 6));
                            }
                        }
                    }
                    else if (!line.StartsWith("Content-Type:") && !string.IsNullOrWhiteSpace(line))
                    {
                        // This is the field value
                        fieldValue.AppendLine(line.Trim());
                    }
                }

                if (!string.IsNullOrEmpty(fieldName) && fieldValue.Length > 0)
                {
                    formData[fieldName] = fieldValue.ToString().Trim();
                }
            }

            return formData;
        }

        public static async Task<byte[]> ReadFileDataAsync(HttpRequestData req)
        {
            try
            {
                req.Body.Position = 0;
                using var memoryStream = new MemoryStream();
                await req.Body.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to read file data from request", ex);
            }
        }

        public static string ExtractBoundary(string contentType)
        {
            if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/form-data"))
            {
                throw new ArgumentException("Content type is not multipart/form-data");
            }

            var boundaryIndex = contentType.IndexOf("boundary=");
            if (boundaryIndex == -1)
            {
                throw new ArgumentException("No boundary found in content type");
            }

            var boundary = contentType.Substring(boundaryIndex + 9).Trim();

            // Handle various boundary formats
            if (boundary.StartsWith("\"") && boundary.EndsWith("\""))
            {
                boundary = boundary.Substring(1, boundary.Length - 2);
            }

            return boundary.Trim();
        }

        public static async Task<FormDataResult> ParseFormDataWithFile(HttpRequestData req)
        {
            var result = new FormDataResult();

            try
            {
                var formData = await ParseMultipartFormData(req);
                var fileData = await ReadFileDataAsync(req);

                result.FormFields = formData;
                result.FileData = fileData;
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.IsSuccess = false;
            }

            return result;
        }
    }

    public class FormDataResult
    {
        public Dictionary<string, string> FormFields { get; set; } = new Dictionary<string, string>();
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}