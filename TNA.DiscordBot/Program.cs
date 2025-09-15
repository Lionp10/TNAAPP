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

            // Usar fecha completa UTC del día actual y tomar el día anterior completo como rango
            var endDateUtc = DateTimeOffset.UtcNow.Date;      // ej: 2025-09-15T00:00:00Z
            var startDateUtc = endDateUtc.AddDays(-1);        // día anterior completo
            var displayDate = endDateUtc.ToString("dd/MM/yyyy"); // para título (sin "UTC")

            // Esperar para que caché de canales se estabilice
            await Task.Delay(2000);

            // Obtener ranking del último día: [startDateUtc, endDateUtc)
            List<TNA.BLL.DTOs.PlayerRankingDTO> ranking = new();
            try
            {
                using var scope = host.Services.CreateScope();
                var playerMatchService = scope.ServiceProvider.GetRequiredService<IPlayerMatchService>();
                var db = scope.ServiceProvider.GetRequiredService<TNADbContext>();

                // DIAGNÓSTICO: contar PlayerMatches en la BD y en el rango
                var allMatches = await db.PlayerMatches.AsNoTracking().ToListAsync();
                Console.WriteLine($"[DIAG] PlayerMatches total en BD: {allMatches.Count}");

                var matchesInRange = allMatches
                    .Where(pm =>
                    {
                        if (DateTimeOffset.TryParse(pm.MatchCreatedAt, out var dt)) return dt >= startDateUtc && dt < endDateUtc;
                        return false;
                    })
                    .ToList();

                Console.WriteLine($"[DIAG] PlayerMatches en el rango [{startDateUtc:o} - {endDateUtc:o}): {matchesInRange.Count}");

                // Llamada real al servicio
                ranking = await playerMatchService.GetRankingAsync(startDateUtc, endDateUtc);
                Console.WriteLine($"[DIAG] Ranking obtenido por service: {ranking?.Count ?? 0} jugadores");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error obteniendo ranking desde DB: " + ex);
            }

            // Intentar obtener el canal
            IMessageChannel? channel = null;
            try
            {
                channel = client.GetChannel(channelId) as IMessageChannel;
                if (channel == null)
                {
                    foreach (var g in client.Guilds)
                    {
                        try
                        {
                            var textCh = g.GetTextChannel(channelId);
                            if (textCh != null)
                            {
                                channel = textCh;
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error buscando canal: " + ex);
            }

            if (channel == null)
            {
                Console.WriteLine("❌ No se encontró el canal especificado. Revisa ID/permiso/bot en servidor.");
                if (readyTcs != null) readyTcs.TrySetResult(true);
                return;
            }

            try
            {
                if (ranking == null || ranking.Count == 0)
                {
                    var info = $"📢 Ranking diario ({displayDate}) — Total: 0 jugadores\nNo se encontraron partidas en el día anterior ({startDateUtc:dd/MM/yyyy}).";
                    await channel.SendMessageAsync(info);
                    Console.WriteLine("[DIAG] Ranking vacío, enviado aviso.");
                }
                else
                {
                    // Enviar título sin "UTC"
                    var header = $"📢 Ranking diario ({displayDate}) — Total: {ranking.Count} jugadores";
                    await channel.SendMessageAsync(header);

                    // Construir y enviar tabla (columnas: #, Nickname, Partidas, Kills, Daño, Puntos)
                    var messages = BuildTableMessagesOrdered(ranking);
                    foreach (var msg in messages)
                    {
                        await channel.SendMessageAsync(msg);
                        await Task.Delay(200);
                    }

                    // Mensaje final con enlace
                    var footer = "Para ver más detalles y diferentes rankings, visitá www.tnaesport.somee.com";
                    await channel.SendMessageAsync(footer);

                    Console.WriteLine("📤 Mensajes de ranking enviados correctamente.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error enviando el ranking: " + ex);
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
                await readyTcs.Task.WaitAsync(TimeSpan.FromSeconds(45));
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

    // Build messages with order: #, Nickname, Partidas, Kills, Daño, Puntos
    static List<string> BuildTableMessagesOrdered(List<TNA.BLL.DTOs.PlayerRankingDTO> ranking)
    {
        const int MaxMessageLen = 1900;
        int posW = 3;
        int nickMax = Math.Min(40, ranking.Max(r => (r.PlayerNickname ?? r.PlayerId).Length));
        nickMax = Math.Max(8, nickMax);
        int matchesW = 7;
        int killsW = 6;
        int damageW = 10;
        int ptsW = 6;

        string header = $"{ "#".PadRight(posW)} | { "Nickname".PadRight(nickMax)} | { "Partidas".PadLeft(matchesW)} | { "Kills".PadLeft(killsW)} | { "Daño".PadLeft(damageW)} | { "Puntos".PadLeft(ptsW)}";
        string sep = new string('-', header.Length);

        var messages = new List<string>();
        var sb = new StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine(sep);

        int pos = 1;
        foreach (var p in ranking)
        {
            var nick = string.IsNullOrWhiteSpace(p.PlayerNickname) ? p.PlayerId : p.PlayerNickname;
            if (nick.Length > nickMax) nick = nick.Substring(0, nickMax - 1) + "…";

            var partidas = p.MatchesCount.ToString();
            var kills = p.TotalKills.ToString();
            var dano = Math.Round(p.TotalDamageDealt, 0).ToString();
            var puntos = p.TotalPoints.ToString("F2");

            var line = $"{pos.ToString().PadRight(posW)} | {nick.PadRight(nickMax)} | {partidas.PadLeft(matchesW)} | {kills.PadLeft(killsW)} | {dano.PadLeft(damageW)} | {puntos.PadLeft(ptsW)}";

            if (sb.Length + line.Length + 10 > MaxMessageLen)
            {
                messages.Add("```" + sb.ToString().TrimEnd() + "```");
                sb.Clear();
                sb.AppendLine(header);
                sb.AppendLine(sep);
            }

            sb.AppendLine(line);
            pos++;
        }

        if (sb.Length > 0)
            messages.Add("```" + sb.ToString().TrimEnd() + "```");

        return messages;
    }
}
