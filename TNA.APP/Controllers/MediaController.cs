using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TNA.BLL.Services.Interfaces;

namespace TNA.APP.Controllers
{
    [Route("Media")]
    public class MediaController : Controller
    {
        private readonly IS3Service _s3Service;
        private readonly ILogger<MediaController> _logger;

        public MediaController(IS3Service s3Service, ILogger<MediaController> logger)
        {
            _s3Service = s3Service ?? throw new ArgumentNullException(nameof(s3Service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("Image")]
        [AllowAnonymous]
        public async Task<IActionResult> Image(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("key is required");

            try
            {
                var (exists, error) = await _s3Service.TryHeadObjectAsync(key);
                if (!exists)
                {
                    _logger.LogWarning("Media.Image: object not found or inaccessible. Key={Key} Error={Error}", key, error ?? "<none>");
                    return NotFound();
                }

                var (stream, contentType) = await _s3Service.GetFileStreamAsync(key, cancellationToken).ConfigureAwait(false);

                Response.Headers["Cache-Control"] = "public, max-age=3600";

                return File(stream, string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving media for Key={Key}", key);
                return StatusCode(500);
            }
        }
    }
}
