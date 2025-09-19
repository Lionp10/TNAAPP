using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TNA.BLL.DTOs;
using TNA.BLL.Services.Interfaces;

namespace TNA.BLL.Services.Implementations
{
    public class S3Service : IS3Service
    {
        private readonly AwsS3OptionsDTO _options;
        private readonly IAmazonS3 _s3Client;

        public S3Service(IOptions<AwsS3OptionsDTO> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_options.Region)
            };

            if (!string.IsNullOrWhiteSpace(_options.ServiceUrl))
            {
                config.ServiceURL = _options.ServiceUrl;
                config.ForcePathStyle = true;
            }

            if (!string.IsNullOrWhiteSpace(_options.AccessKeyId) && !string.IsNullOrWhiteSpace(_options.SecretAccessKey))
            {
                var creds = new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey);
                _s3Client = new AmazonS3Client(creds, config);
            }
            else
            {
                _s3Client = new AmazonS3Client(config);
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            var extension = Path.GetExtension(file.FileName);
            var key = $"profiles/{Guid.NewGuid()}{extension}";

            using var stream = file.OpenReadStream();
            var putRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType
            };

            var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK && response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new InvalidOperationException("Error subiendo archivo a S3: " + response.HttpStatusCode);
            }

            return key;
        }

        public Task<string> GeneratePreSignedUrlAsync(string key, TimeSpan? expiry = null)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            var e = expiry ?? TimeSpan.FromHours(1);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                Expires = DateTime.UtcNow.Add(e),
                Verb = HttpVerb.GET
            };

            var url = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(url);
        }

        public async Task<(bool Exists, string? ErrorMessage)> TryHeadObjectAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            try
            {
                var request = new GetObjectMetadataRequest
                {
                    BucketName = _options.BucketName,
                    Key = key
                };

                var response = await _s3Client.GetObjectMetadataAsync(request).ConfigureAwait(false);
                return (true, null);
            }
            catch (AmazonS3Exception ex)
            {
                return (false, $"AWS S3 Error: {ex.StatusCode} - {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Error inesperado: {ex.Message}");
            }
        }

        public async Task<(Stream Stream, string ContentType)> GetFileStreamAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            var request = new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            using var response = await _s3Client.GetObjectAsync(request, cancellationToken).ConfigureAwait(false);
            var ms = new MemoryStream();
            await response.ResponseStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            ms.Position = 0;

            string contentType = "application/octet-stream";
            try
            {
                var ct = response.Headers["Content-Type"];
                if (!string.IsNullOrWhiteSpace(ct)) contentType = ct;
            }
            catch { }

            return (ms, contentType);
        }

        public async Task DeleteObjectAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));

            try
            {
                var request = new DeleteObjectRequest
                {
                    BucketName = _options.BucketName,
                    Key = key
                };

                var response = await _s3Client.DeleteObjectAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonS3Exception ex)
            {
                throw new InvalidOperationException($"AWS S3 Error al eliminar objeto: {ex.StatusCode} - {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error inesperado al eliminar objeto S3: {ex.Message}", ex);
            }
        }
    }
}
