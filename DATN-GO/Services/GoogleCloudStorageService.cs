using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Để làm việc với IFormFile

namespace DATN_GO.Service
{
    public class GoogleCloudStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly ILogger<GoogleCloudStorageService> _logger; // Thêm logger

        public GoogleCloudStorageService(IConfiguration configuration, ILogger<GoogleCloudStorageService> logger)
        {
            _logger = logger;
            _bucketName = configuration["GoogleCloudStorage:BucketName"] ?? throw new ArgumentNullException("GoogleCloudStorage:BucketName not configured.");
            string credentialsPath = configuration["GoogleCloudStorage:CredentialsPath"] ?? throw new ArgumentNullException("GoogleCloudStorage:CredentialsPath not configured.");

            // Đặt biến môi trường cho Google Cloud Client Library
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", Path.Combine(Directory.GetCurrentDirectory(), credentialsPath));

            _storageClient = StorageClient.Create();
            _logger.LogInformation("GoogleCloudStorageService initialized.");
        }

        public async Task<string?> UploadFileAsync(IFormFile file, string folderName = "")
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Attempted to upload a null or empty file.");
                return null;
            }

            try
            {
                string fileExtension = Path.GetExtension(file.FileName);
                string fileName = $"{folderName}{Guid.NewGuid()}{fileExtension}";

                // Upload file
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    var gcsObject = await _storageClient.UploadObjectAsync(
                        _bucketName,
                        fileName,
                        file.ContentType,
                        stream
                    );

                  
                    string publicUrl = $"https://storage.googleapis.com/{_bucketName}/{fileName}";
                    _logger.LogInformation($"File uploaded successfully to: {publicUrl}");
                    return publicUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading file to GCS: {ex.Message}");
                return null;
            }
        }
    }
}