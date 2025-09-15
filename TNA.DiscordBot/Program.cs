using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Linq;
using TNA.BLL.Services.Interfaces;
using TNA.BLL.Services.Implementations;
using TNA.DAL.DbContext;
using TNA.DAL.Repositories.Implementations;
using TNA.DAL.Repositories.Interfaces;

class Program
{
    static async Task Main(string[] args)
    {
        bool runOnce = args.Contains("--run-once");

        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        var channelIdString = Environment.GetEnvironmentVariable("DISCORD_CHANNEL_ID");
        var dbConn = Environment.GetEnvironmentVariable("DATABASE_CONNECTION");

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(channelIdString))
        {
            Console.WriteLine("❌ No se encontró DISCORD_TOKEN o DISCORD_CHANNEL_ID en variables de entorno.");
            return;
        }

        if (!ulong.TryParse(channelIdString, out var channelId))
        {
            Console.WriteLine("❌ DISCORD_CHANNEL_ID no es un número válido.");
            return;
        }

        if (string.IsNullOrWhiteSpace(dbConn))
        {
            Console.WriteLine("❌ No se encontró DATABASE_CONNECTION en variables de entorno. Necesaria para leer el ranking desde la BD.");
            return;
        }

        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
            {
                services.AddDbContext<TNADbContext>(o => o.UseSqlServer(dbConn));
                services.AddScoped<IPlayerMatchRepository, PlayerMatchRepository>();
                services.AddScoped<IClanMemberRepository, ClanMemberRepository>();
                services.AddScoped<IPlayerMatchService, PlayerMatchService>();
            })
            .Build();

        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
        };

        var client = new DiscordSocketClient(config);

        client.Log += msg =>
        {
            Console.WriteLine($"[Discord.Net] {msg}");
            return Task.CompletedTask;
        };

        TaskCompletionSource<bool>? readyTcs = runOnce ? new(TaskCreationOptions.RunContinuationsAsynchronously) : null;

        client.Ready += async () =>
        {
            Console.WriteLine($"✅ {client.CurrentUser} conectado a Discord.");

            // Esperar para que caché de canales se estabilice
            await Task.Delay(2000);

            // Obtener ranking del último día: [UtcNow.AddDays(-1), UtcNow)
            List<TNA.BLL.DTOs.PlayerRankingDTO> ranking = new();
            try
            {
                using var scope = host.Services.CreateScope();
                var playerMatchService = scope.ServiceProvider.GetRequiredService<IPlayerMatchService>();
                var db = scope.ServiceProvider.GetRequiredService<TNADbContext>();

                var end = DateTimeOffset.UtcNow;
                var start = end.AddDays(-1);

                // DIAGNÓSTICO: contar PlayerMatches en la BD y en el rango
                var allMatches = await db.PlayerMatches.AsNoTracking().ToListAsync();
                Console.WriteLine($"[DIAG] PlayerMatches total en BD: {allMatches.Count}");

                var matchesInRange = allMatches
                    .Where(pm =>
                    {
                        if (DateTimeOffset.TryParse(pm.MatchCreatedAt, out var dt)) return dt >= start && dt < end;
                        return false;
                    })
                    .ToList();

                Console.WriteLine($"[DIAG] PlayerMatches en el rango [{start:o} - {end:o}): {matchesInRange.Count}");
                if (matchesInRange.Count > 0)
                {
                    foreach (var m in matchesInRange.Take(5))
                        Console.WriteLine($"[DIAG] Sample match: PlayerId={m.PlayerId} MatchId={m.MatchId} CreatedAt={m.MatchCreatedAt}");
                }
                else
                {
                    Console.WriteLine("[DIAG] No hay matches en la BD dentro del rango. Revisar formato de MatchCreatedAt o el rango temporal.");
                }

                // Llamada real al servicio (para comparar)
                ranking = await playerMatchService.GetRankingAsync(start, end);
                Console.WriteLine($"[DIAG] Ranking obtenido por service: {ranking?.Count ?? 0} jugadores");
                if (ranking != null && ranking.Count > 0)
                {
                    foreach (var p in ranking.Take(3))
                        Console.WriteLine($"[DIAG] Top service: {p.PlayerId} / {p.PlayerNickname} => {p.TotalPoints}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error obteniendo ranking desde DB: " + ex);
            }

            // Intentar obtener el canal de varias formas y mostrar diagnóstico
            IMessageChannel? channel = null;
            try
            {
                channel = client.GetChannel(channelId) as IMessageChannel;
                Console.WriteLine($"[DIAG] client.GetChannel({channelId}) returned {(channel == null ? "null" : "non-null")}");

                if (channel == null)
                {
                    // Buscar en guilds por si no está en caché global
                    foreach (var g in client.Guilds)
                    {
                        try
                        {
                            var textCh = g.GetTextChannel(channelId);
                            if (textCh != null)
                            {
                                channel = textCh;
                                Console.WriteLine($"[DIAG] Canal encontrado en guild {g.Id} / {g.Name}");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DIAG] Error buscando canal en guild {g.Id}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error buscando canal: " + ex);
            }

            if (channel != null)
            {
                try
                {
                    if (ranking is null || ranking.Count == 0)
                    {
                        var info = $"📢 Ranking diario ({DateTimeOffset.UtcNow:dd/MM/yyyy} UTC): no se encontraron partidas en las últimas 24h.";
                        Console.WriteLine("[DIAG] Ranking vacío, enviando mensaje informativo.");
                        await channel.SendMessageAsync(info);
                    }
                    else
                    {
                        // Enviar título
                        var header = $"📢 Ranking diario ({DateTimeOffset.UtcNow:dd/MM/yyyy} UTC) — Total: {ranking.Count} jugadores";
                        await channel.SendMessageAsync(header);

                        // Construir mensajes en formato tabla y enviarlos por chunks
                        var messages = BuildTableMessages(ranking);
                        foreach (var msg in messages)
                        {
                            await channel.SendMessageAsync(msg);
                            // Pequeña pausa para evitar rate limits si envías muchos mensajes
                            await Task.Delay(250);
                        }
                        Console.WriteLine("📤 Mensajes de ranking enviados correctamente.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Error enviando el mensaje: " + ex);
                }
            }
            else
            {
                Console.WriteLine("❌ No se encontró el canal especificado en caché ni en los guilds. Posibles causas:");
                Console.WriteLine("  - El bot no está en el servidor correcto.");
                Console.WriteLine("  - El ID del canal es incorrecto.");
                Console.WriteLine("  - Permisos del bot: no puede ver el canal.");
                Console.WriteLine("  - La caché aún no se ha poblado (intenta aumentar el delay).");
            }

            if (readyTcs != null)
            {
                readyTcs.TrySetResult(true);
            }
        };

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        if (runOnce && readyTcs != null)
        {
            try
            {
                await readyTcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (TimeoutException)
            {
                Console.WriteLine("❌ Timeout esperando al evento Ready.");
            }

            try
            {
                await client.LogoutAsync();
                await client.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Error durante logout/stop: " + ex);
            }

            return;
        }

        if (!runOnce)
            await Task.Delay(-1);
    }

    // Helper: crea una lista de mensajes (cada uno dentro de ``` ```), con formato tipo tabla.
    // Se encarga de partir en chunks < ~2000 caracteres para Discord.
    static List<string> BuildTableMessages(List<TNA.BLL.DTOs.PlayerRankingDTO> ranking)
    {
        const int MaxMessageLen = 1900; // margen de seguridad
        // Column widths
        int posW = 3;
        int ptsW = 6;
        int matchesW = 7;
        int killsW = 5;
        int nickMax = Math.Min(40, ranking.Max(r => (r.PlayerNickname ?? r.PlayerId ?? r.PlayerId).Length));
        nickMax = Math.Max(8, nickMax);

        string headerLine = $"{"#".PadRight(posW)} | {"Nickname".PadRight(nickMax)} | {"Pts".PadLeft(ptsW)} | {"Matches".PadLeft(matchesW)} | {"Kills".PadLeft(killsW)}";
        string separator = new string('-', headerLine.Length);

        var messages = new List<string>();
        var sb = new StringBuilder();
        sb.AppendLine(headerLine);
        sb.AppendLine(separator);

        int pos = 1;
        foreach (var p in ranking)
        {
            var nick = string.IsNullOrWhiteSpace(p.PlayerNickname) ? p.PlayerId : p.PlayerNickname;
            if (nick.Length > nickMax) nick = nick.Substring(0, nickMax - 1) + "…";

            var ptsStr = (p.TotalPoints).ToString("F2");
            var matchesStr = p.MatchesCount.ToString();
            var killsStr = p.TotalKills.ToString();

            var line = $"{pos.ToString().PadRight(posW)} | {nick.PadRight(nickMax)} | {ptsStr.PadLeft(ptsW)} | {matchesStr.PadLeft(matchesW)} | {killsStr.PadLeft(killsW)}";
            // Si excede el chunk, cerrar y empezar uno nuevo
            if (sb.Length + line.Length + 10 > MaxMessageLen) // +10 para las triple backticks
            {
                var full = "```" + sb.ToString().TrimEnd() + "```";
                messages.Add(full);
                sb.Clear();
                sb.AppendLine(headerLine);
                sb.AppendLine(separator);
            }

            sb.AppendLine(line);
            pos++;
        }

        if (sb.Length > 0)
        {
            var full = "```" + sb.ToString().TrimEnd() + "```";
            messages.Add(full);
        }

        return messages;
    }
}
