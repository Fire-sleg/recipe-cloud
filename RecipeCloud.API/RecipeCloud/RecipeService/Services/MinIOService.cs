using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;

namespace RecipeService.Services
{
    public class MinIOService : IMinIOService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public MinIOService(IConfiguration configuration)
        {
            var s3Config = new AmazonS3Config
            {
                ServiceURL = configuration["MinIO:ServiceURL"], // e.g., http://localhost:9000
                ForcePathStyle = true
            };
            _s3Client = new AmazonS3Client(
                configuration["MinIO:AccessKey"],
                configuration["MinIO:SecretKey"],
                s3Config);
            _bucketName = configuration["MinIO:BucketName"];

            EnsureBucketExistsAsync().GetAwaiter().GetResult();
        }

        private async Task EnsureBucketExistsAsync()
        {
            try
            {
                // Перевіряємо, чи існує бакет
                var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);

                if (!bucketExists)
                {
                    // Створюємо бакет, якщо він не існує
                    var putBucketRequest = new PutBucketRequest
                    {
                        BucketName = _bucketName,
                        UseClientRegion = true
                    };

                    await _s3Client.PutBucketAsync(putBucketRequest);

                    // Налаштовуємо політику публічного доступу для читання
                    await SetBucketPolicyAsync();
                }
            }
            catch (AmazonS3Exception ex)
            {
                // Логуємо помилку, але не припиняємо роботу сервісу
                Console.WriteLine($"Error creating bucket: {ex.Message}");
                throw;
            }
        }

        private async Task SetBucketPolicyAsync()
        {
            var bucketPolicy = $@"{{
                ""Version"": ""2012-10-17"",
                ""Statement"": [
                    {{
                        ""Effect"": ""Allow"",
                        ""Principal"": ""*"",
                        ""Action"": ""s3:GetObject"",
                        ""Resource"": ""arn:aws:s3:::{_bucketName}/*""
                    }}
                ]
            }}";

            var putPolicyRequest = new PutBucketPolicyRequest
            {
                BucketName = _bucketName,
                Policy = bucketPolicy
            };

            try
            {
                await _s3Client.PutBucketPolicyAsync(putPolicyRequest);
            }
            catch (AmazonS3Exception ex)
            {
                Console.WriteLine($"Warning: Could not set bucket policy: {ex.Message}");
                // Не кидаємо виняток, оскільки це не критична помилка
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = file.OpenReadStream(),
                ContentType = file.ContentType
            };

            putRequest.CannedACL = S3CannedACL.PublicRead;

            await _s3Client.PutObjectAsync(putRequest);

            return $"{_s3Client.Config.ServiceURL}/{_bucketName}/{fileName}";
        }

        public async Task DeleteImageAsync(string fileName)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName
            };
            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
    }
}