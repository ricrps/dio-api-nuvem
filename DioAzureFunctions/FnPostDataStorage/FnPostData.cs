using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FnPostDataStorage
{
    public class FnPostData
    {

        [Function("fnPostData")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, ILogger logger)
        {
            logger.LogInformation("fnPostDataStorage triggered.");

            // Check if request has a file
            if (!req.HasFormContentType || req.Form.Files.Count == 0)
            {
                return new BadRequestObjectResult("No file found in the request.");
            }

            var file = req.Form.Files[0];

            // Check file size (limit 100MB)
            const long maxFileSize = 100 * 1024 * 1024; // 100 MB
            if (file.Length > maxFileSize)
            {
                return new BadRequestObjectResult($"File size exceeds the 100MB limit. Actual size: {file.Length / (1024 * 1024)} MB.");
            }

            // Retrieve Storage Account connection string from environment variables
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            if (string.IsNullOrEmpty(connectionString))
            {
                return new ObjectResult("Azure Storage connection string is not configured.") { StatusCode = 500 };
            }

            // Specify the container name
            string containerName = "uploads";

            try
            {
                // Create BlobServiceClient and BlobContainerClient
                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                // Ensure the container exists
                await containerClient.CreateIfNotExistsAsync();

                // Set the file name in the blob storage
                string blobName = Path.GetFileName(file.FileName);
                BlobClient blobClient = containerClient.GetBlobClient(blobName);

                // Upload file to blob storage
                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, true);
                }

                logger.LogInformation($"File uploaded successfully: {blobName}");
                return new OkObjectResult(new { Message = "File uploaded successfully.", FileName = blobName });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading file to blob storage.");
                return new ObjectResult("Error uploading file to blob storage.") { StatusCode = 500 };
            }
        }
    }
}
