using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TNA.BLL.Services.Interfaces
{
    public interface IS3Service
    {
        Task<string> UploadFileAsync(IFormFile file, CancellationToken cancellationToken = default);
        Task<string> GeneratePreSignedUrlAsync(string key, TimeSpan? expiry = null);
        Task<(bool Exists, string? ErrorMessage)> TryHeadObjectAsync(string key);
        Task<(System.IO.Stream Stream, string ContentType)> GetFileStreamAsync(string key, CancellationToken cancellationToken = default);
        Task DeleteObjectAsync(string key, CancellationToken cancellationToken = default);
    }
}
