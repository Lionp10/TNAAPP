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
                        // 1) Embed resumen Top 10 (puntos en negrita)
                        var top = ranking.Take(10).ToList();
                        var embed = new EmbedBuilder()
                            .WithTitle($"📢 Ranking diario ({DateTimeOffset.UtcNow:dd/MM/yyyy} UTC) — Top {top.Count}")
                            .WithColor(Color.DarkBlue)
                            .WithFooter($"Total jugadores: {ranking.Count}");

                        int pos = 1;
                        foreach (var p in top)
                        {
                            var nick = string.IsNullOrWhiteSpace(p.PlayerNickname) ? p.PlayerId : p.PlayerNickname;
                            string fieldName = $"#{pos}  {nick}";
                            string fieldValue =
                                $"Partidas: {p.MatchesCount} • Derr: {p.TotalDBNOs} • Asist: {p.TotalAssists} • K: {p.TotalKills}\n" +
                                $"HS: {p.TotalHeadshotsKills} • Daño: {p.TotalDamageDealt:F0} • Rev: {p.TotalRevives} • TK: {p.TotalTeamKill}\n" +
                                $"Tiempo: {FormatTime(p.TotalTimeSurvived)} • Pos.prom: {p.AverageWinPlace}  • **{p.TotalPoints:F2} pts**";
                            embed.AddField(fieldName, fieldValue, inline: false);
                            pos++;
                        }

                        await channel.SendMessageAsync(embed: embed.Build());

                        // 2) Archivo con la tabla completa (monoespaciada)
                        var fullText = BuildFullTableText(ranking);
                        var bytes = Encoding.UTF8.GetBytes(fullText);
                        using var ms = new System.IO.MemoryStream(bytes);
                        await channel.SendFileAsync(ms, "ranking.txt", $"Ranking completo ({DateTimeOffset.UtcNow:dd/MM/yyyy} UTC) — Total: {ranking.Count} jugadores");
                        Console.WriteLine("[DIAG] Embed top y archivo con tabla completa enviados.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Error enviando el ranking: " + ex);
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

    static string FormatTime(decimal secondsDecimal)
    {
        try
        {
            var seconds = (double)secondsDecimal;
            var ts = TimeSpan.FromSeconds(seconds);
            // Si el valor es muy grande, mostrar días y hh:mm:ss
            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}d {ts:hh\\:mm\\:ss}";
            return ts.ToString(@"hh\:mm\:ss");
        }
        catch
        {
            return "00:00:00";
        }
    }

    // Construye tabla completa en texto monoespaciado
    static string BuildFullTableText(List<TNA.BLL.DTOs.PlayerRankingDTO> ranking)
    {
        // Column widths
        int posW = 3;
        int nickW = Math.Min(40, Math.Max(10, ranking.Max(r => (r.PlayerNickname ?? r.PlayerId).Length)));
        int matchesW = 7;
        int dbnoW = 6;
        int assistsW = 8;
        int killsW = 6;
        int hsW = 8;
        int dmgW = 10;
        int revW = 6;
        int tkW = 8;
        int timeW = 11;
        int winW = 5;
        int ptsW = 6;

        string header = $"{ "#".PadRight(posW)} | { "Jugador".PadRight(nickW)} | { "Partidas".PadLeft(matchesW)} | { "Derribos".PadLeft(dbnoW)} | { "Asist".PadLeft(assistsW)} | { "Kills".PadLeft(killsW)} | { "Headshots".PadLeft(hsW)} | { "Daño".PadLeft(dmgW)} | { "Revives".PadLeft(revW)} | { "TeamK".PadLeft(tkW)} | { "Tiempo".PadLeft(timeW)} | { "Pos." .PadLeft(winW)} | { "Puntos".PadLeft(ptsW)}";
        var sep = new string('-', header.Length);

        var sb = new StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine(sep);

        int pos = 1;
        foreach (var p in ranking)
        {
            var nick = string.IsNullOrWhiteSpace(p.PlayerNickname) ? p.PlayerId : p.PlayerNickname;
            if (nick.Length > nickW) nick = nick.Substring(0, nickW - 1) + "…";

            var line =
                $"{pos.ToString().PadRight(posW)} | " +
                $"{nick.PadRight(nickW)} | " +
                $"{p.MatchesCount.ToString().PadLeft(matchesW)} | " +
                $"{p.TotalDBNOs.ToString().PadLeft(dbnoW)} | " +
                $"{p.TotalAssists.ToString().PadLeft(assistsW)} | " +
                $"{p.TotalKills.ToString().PadLeft(killsW)} | " +
                $"{p.TotalHeadshotsKills.ToString().PadLeft(hsW)} | " +
                $"{Math.Round(p.TotalDamageDealt, 0).ToString().PadLeft(dmgW)} | " +
                $"{p.TotalRevives.ToString().PadLeft(revW)} | " +
                $"{p.TotalTeamKill.ToString().PadLeft(tkW)} | " +
                $"{FormatTime(p.TotalTimeSurvived).PadLeft(timeW)} | " +
                $"{p.AverageWinPlace.ToString().PadLeft(winW)} | " +
                $"{p.TotalPoints.ToString("F2").PadLeft(ptsW)}";

            sb.AppendLine(line);
            pos++;
        }

        return sb.ToString();
    }
}
