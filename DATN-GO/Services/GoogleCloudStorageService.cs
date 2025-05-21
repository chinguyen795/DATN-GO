using Google.Cloud.Storage.V1;

public class GoogleCloudStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly ILogger<GoogleCloudStorageService> _logger;
    public async Task<string?> UploadFileAsync(IFormFile file, string folderName = "")
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("⚠️ Attempted to upload null or empty file.");
            return null;
        }

        try
        {
            string fileExtension = Path.GetExtension(file.FileName);
            string fileName = $"{folderName.TrimEnd('/')}/{Guid.NewGuid()}{fileExtension}";

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            await _storageClient.UploadObjectAsync(
                _bucketName,
                fileName,
                "application/octet-stream",
                stream
            );

            string publicUrl = $"https://storage.googleapis.com/{_bucketName}/{fileName}";
            _logger.LogInformation("✅ File uploaded to: {Url}", publicUrl);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Upload failed.");
            return null;
        }
    }
    public GoogleCloudStorageService(IConfiguration configuration, ILogger<GoogleCloudStorageService> logger)
    {
        _logger = logger;

        _bucketName = configuration["GoogleCloudStorage:BucketName"]
                      ?? throw new ArgumentNullException("GoogleCloudStorage:BucketName not configured.");

        string credentialsPath = configuration["GoogleCloudStorage:CredentialsPath"]
                                 ?? throw new ArgumentNullException("GoogleCloudStorage:CredentialsPath not configured.");

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), credentialsPath);

        if (!File.Exists(fullPath))
        {
            _logger.LogError("❌ Không tìm thấy file credentials tại: {Path}", fullPath);
            throw new FileNotFoundException($"Không tìm thấy file credentials tại: {fullPath}");
        }

        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", fullPath);
        _storageClient = StorageClient.Create();

        _logger.LogInformation("✅ GoogleCloudStorageService khởi tạo thành công với bucket: {Bucket} | credentials: {Path}", _bucketName, fullPath);
    }






    public async Task<bool> DeleteFileAsync(string url)
    {
        try
        {
            var uri = new Uri(url);
            var objectName = uri.AbsolutePath.TrimStart('/').Replace($"{_bucketName}/", "");

            await _storageClient.DeleteObjectAsync(_bucketName, objectName);
            _logger.LogInformation("🗑️ Deleted file: {ObjectName}", objectName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting object.");
            return false;
        }
    }
}
