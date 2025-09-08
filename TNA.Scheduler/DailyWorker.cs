using Microsoft.Extensions.Options;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TNA.BLL.Config;
using TNA.BLL.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Hosting;

namespace TNA.Scheduler
{
    public class DailyWorker : BackgroundService
    {
        private readonly ILogger<DailyWorker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly TimeSpan _runTimeOfDay;
        // Fijo a GMT-3 según requisito
        private static readonly TimeSpan SchedulerOffset = TimeSpan.FromHours(-3);

        public DailyWorker(ILogger<DailyWorker> logger, IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime, IOptions<PubgOptions> options, IConfiguration config)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hostApplicationLifetime = hostApplicationLifetime;

            // Leer hora de ejecución desde config (formato "HH:mm")
            var raw = config.GetValue<string>("Scheduler:DailyTime") ?? config["Scheduler:DailyTime"];
            raw = raw?.Trim();

            _logger.LogInformation("Scheduler:DailyTime raw value = '{RawSchedule}'", raw ?? "(null)");

            // Intentos robustos de parseo: TryParseExact -> TryParse (acepta H:mm, hh:mm, etc.)
            if (!string.IsNullOrEmpty(raw) &&
                TimeSpan.TryParseExact(raw, @"hh\:mm", CultureInfo.InvariantCulture, out var parsed) ||
                (!string.IsNullOrEmpty(raw) && TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out parsed)))
            {
                _runTimeOfDay = parsed;
                _logger.LogInformation("Parsed schedule time: {TimeOfDay}", _runTimeOfDay);
            }
            else
            {
                // fallback a 02:00 si todo falla
                _runTimeOfDay = TimeSpan.FromHours(2);
                _logger.LogWarning("Could not parse schedule '{RawSchedule}', falling back to {Fallback}", raw ?? "(null)", _runTimeOfDay);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var runOnce = Environment.GetEnvironmentVariable("RUN_ONCE");

            if (!string.IsNullOrEmpty(runOnce) && runOnce.ToLower() == "true")
            {
                _logger.LogInformation("RUN_ONCE mode detected. Executing immediately...");

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var pubgService = scope.ServiceProvider.GetRequiredService<IPubgService>();

                    _logger.LogInformation("Starting statistics update (RUN_ONCE mode).");
                    await pubgService.UpdateStatisticsAsync(stoppingToken);
                    _logger.LogInformation("Statistics update finished successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during statistics update.");
                    throw; // Re-throw para que GitHub Actions detecte el error
                }

                _logger.LogInformation("RUN_ONCE execution completed. Stopping application.");
                _hostApplicationLifetime.StopApplication();
                return;
            }

            _logger.LogInformation("DailyWorker started. Will run daily at {TimeOfDay} (GMT-3).", _runTimeOfDay);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var delay = GetDelayUntilNextRunUtc(_runTimeOfDay, SchedulerOffset);
                    _logger.LogInformation("Next run in {Delay}.", delay);
                    await Task.Delay(delay, stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var pubgService = scope.ServiceProvider.GetRequiredService<IPubgService>();

                    _logger.LogInformation("Starting daily statistics update.");
                    await pubgService.UpdateStatisticsAsync(stoppingToken);
                    _logger.LogInformation("Daily statistics update finished.");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("DailyWorker canceled.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scheduled update. Will retry next scheduled time.");
                    // esperar hasta la próxima ejecución normal
                }
            }

            _logger.LogInformation("DailyWorker stopping.");
        }

        /// <summary>
        /// Calcula el tiempo de espera hasta la próxima ejecución programada.
        /// El parámetro timeOfDay se interpreta en la zona horaria indicada por tzOffset (p. ej. GMT-3).
        /// Devuelve un TimeSpan relativo a DateTimeOffset.UtcNow.
        /// </summary>
        private static TimeSpan GetDelayUntilNextRunUtc(TimeSpan timeOfDay, TimeSpan tzOffset)
        {
            var nowUtc = DateTimeOffset.UtcNow;

            // Obtener la fecha "hoy" en la zona tzOffset (representación del mismo instante en esa zona)
            var todayInTz = nowUtc.ToOffset(tzOffset).Date;

            // Construir el instante programado en la zona tzOffset y convertir a UTC
            var scheduledInTz = new DateTimeOffset(todayInTz + timeOfDay, tzOffset);
            var scheduledUtc = scheduledInTz.ToUniversalTime();

            if (scheduledUtc <= nowUtc)
            {
                scheduledUtc = scheduledUtc.AddDays(1);
            }

            return scheduledUtc - nowUtc;
        }
    }
}
