using TNA.BLL.Services.Interfaces;

namespace TNA.Scheduler
{
    public class DailyWorker : BackgroundService
    {
        private readonly ILogger<DailyWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public DailyWorker(ILogger<DailyWorker> logger, IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DailyWorker started. Ejecutando actualización inmediatamente al arrancar.");

            if (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("DailyWorker cancelado antes de iniciar la tarea.");
                return;
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var pubgService = scope.ServiceProvider.GetRequiredService<IPubgService>();

                _logger.LogInformation("Iniciando actualización inmediata de estadísticas.");
                await pubgService.UpdateStatisticsAsync(stoppingToken);
                _logger.LogInformation("Actualización inmediata finalizada.");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("DailyWorker cancelado durante la ejecución.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la actualización inmediata.");
                throw;
            }

            _logger.LogInformation("DailyWorker completado y se detendrá.");
            _hostApplicationLifetime.StopApplication();
        }
    }
}
