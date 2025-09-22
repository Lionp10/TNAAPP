using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
using TNA.BLL.Services.Implementations;
using TNA.BLL.Services.Interfaces;
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

            var gmtMinus3 = TimeSpan.FromHours(-3);
            var nowInZone = DateTimeOffset.UtcNow.ToOffset(gmtMinus3);
            var startLocal = nowInZone.Date.AddDays(-1);
            var endLocal = startLocal.AddDays(1);        
            var startDateUtc = new DateTimeOffset(startLocal, gmtMinus3).ToUniversalTime();
            var endDateUtc = new DateTimeOffset(endLocal, gmtMinus3).ToUniversalTime();
            var displayDate = startLocal.ToString("dd/MM/yyyy"); 

            await Task.Delay(2000);

            List<TNA.BLL.DTOs.PlayerRankingDTO> ranking = new();
            int totalMembers = 0;
            try
            {
                using var scope = host.Services.CreateScope();
                var playerMatchService = scope.ServiceProvider.GetRequiredService<IPlayerMatchService>();
                var clanMemberRepo = scope.ServiceProvider.GetRequiredService<IClanMemberRepository>();

                ranking = await playerMatchService.GetRankingAsync(startDateUtc, endDateUtc);
                var members = await clanMemberRepo.GetActiveMembersAsync();
                totalMembers = members?.Count ?? 0;

                Console.WriteLine($"[DIAG] Ranking obtenido por service: {ranking?.Count ?? 0} jugadores. Miembros activos totales: {totalMembers}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error obteniendo ranking desde DB: " + ex);
            }

            var playedRanking = (ranking ?? new List<TNA.BLL.DTOs.PlayerRankingDTO>())
                                .Where(r => r.MatchesCount > 0)
                                .ToList();

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
                if (playedRanking == null || playedRanking.Count == 0)
                {
                    var info = $"📢 Ranking diario ({displayDate}) — 0 de {totalMembers} jugadores totales\nNo se encontraron partidas en el día anterior ({startDateUtc:dd/MM/yyyy}).";
                    await channel.SendMessageAsync(info);
                    Console.WriteLine("[DIAG] Ranking vacío (nadie jugó), enviado aviso.");
                }
                else
                {
                    var header = $"📢 Ranking diario ({displayDate}) — {playedRanking.Count} de {totalMembers} jugadores totales";
                    await channel.SendMessageAsync(header);

                    var messages = BuildTableMessagesOrdered(playedRanking);
                    foreach (var msg in messages)
                    {
                        await channel.SendMessageAsync(msg);
                        await Task.Delay(200);
                    }

                    var link = "https://www.tnaesport.somee.com";
                    await channel.SendMessageAsync($"Para ver más detalles y diferentes rankings, visitá {link}");

                    var roleMention = "<@&942961256099352628>";
                    await channel.SendMessageAsync(roleMention);

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

    static List<string> BuildTableMessagesOrdered(List<TNA.BLL.DTOs.PlayerRankingDTO> ranking)
    {
        const int MaxMessageLen = 1900;
        int posW = 3;
        int nickMax = Math.Min(40, ranking.Max(r => (r.PlayerNickname ?? r.PlayerId).Length));
        nickMax = Math.Max(8, nickMax);
        int killsW = 6;
        int damageW = 10;
        int kdaW = 7;

        string header = $"{ "Pos".PadRight(posW)} | { "Nickname".PadRight(nickMax)} | { "Kills".PadLeft(killsW)} | { "Daño".PadLeft(damageW)} | { "KDA".PadLeft(kdaW)}";
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

            var killsStr = p.TotalKills.ToString();
            var damageStr = Math.Round(p.TotalDamageDealt, 0).ToString("F0");

            double kda = 0.0;
            if (p.MatchesCount > 0)
            {
                kda = (double)(p.TotalKills + p.TotalAssists) / Math.Max(1, p.MatchesCount);
            }
            var kdaStr = kda.ToString("F2");

            var line = $"{pos.ToString().PadRight(posW)} | {nick.PadRight(nickMax)} | {killsStr.PadLeft(killsW)} | {damageStr.PadLeft(damageW)} | {kdaStr.PadLeft(kdaW)}";

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
