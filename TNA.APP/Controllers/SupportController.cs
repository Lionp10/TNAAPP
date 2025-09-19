using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TNA.APP.Models;
using TNA.BLL.Services.Interfaces;
using TNA.BLL.DTOs;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace TNA.APP.Controllers
{
    public class SupportController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly EmailDTO _emailSettings;
        private readonly ILogger<SupportController> _logger;

        public SupportController(IEmailService emailService, IOptions<EmailDTO> emailOptions, ILogger<SupportController> logger)
        {
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _emailSettings = emailOptions?.Value ?? throw new ArgumentNullException(nameof(emailOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View(new SupportRequestViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(SupportRequestViewModel model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                string Encode(string s) => WebUtility.HtmlEncode(s ?? string.Empty);
                var body = $@"
                    <p><strong>Nombre:</strong> {Encode(model.Name)} {Encode(model.LastName)}</p>
                    <p><strong>Email remitente:</strong> {Encode(model.Email)}</p>
                    <hr/>
                    <p>{Encode(model.Message).Replace(Environment.NewLine, "<br/>")}</p>
                    <hr/>
                    <p>Enviado desde formulario de contacto web.</p>";

                var to = _emailSettings.FromEmail;

                await _emailService.SendEmailAsync(
                    to,
                    $"Soporte web: {model.Name} {model.LastName}",
                    body,
                    true,
                    model.Attachments,
                    cancellationToken
                ).ConfigureAwait(false);

                TempData["SuccessMessage"] = "Mensaje enviado correctamente. Te responderemos por email lo antes posible.";
                return RedirectToAction(nameof(Contact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando formulario de soporte desde {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Ocurrió un error enviando tu mensaje. Intenta nuevamente más tarde.");
                return View(model);
            }
        }
    }
}
