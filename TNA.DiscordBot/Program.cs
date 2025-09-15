using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text;
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

        // Build a minimal host/service provider to resolve IPlayerMatchService
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
            Console.WriteLine(msg.ToString());
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

                var end = DateTimeOffset.UtcNow;
                var start = end.AddDays(-1);

                ranking = await playerMatchService.GetRankingAsync(start, end);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error obteniendo ranking desde DB: " + ex);
            }

            var channel = client.GetChannel(channelId) as IMessageChannel;
            if (channel != null)
            {
                try
                {
                    string mensaje;
                    if (ranking is null || ranking.Count == 0)
                    {
                        mensaje = $"📢 Ranking diario ({DateTimeOffset.UtcNow:dd/MM/yyyy} UTC): no se encontraron partidas en las últimas 24h.";
                    }
                    else
                    {
                        // Construir texto con top 5
                        var top = ranking.Take(5).ToList();
                        var sb = new StringBuilder();
                        sb.AppendLine($"📢 Ranking diario ({DateTimeOffset.UtcNow:dd/MM/yyyy} UTC) — Top {top.Count}:");
                        int pos = 1;
                        foreach (var p in top)
                        {
                            var nick = string.IsNullOrWhiteSpace(p.PlayerNickname) ? p.PlayerId : p.PlayerNickname;
                            sb.AppendLine($"{pos}. {nick} — {p.TotalPoints:F2} pts — Partidas: {p.MatchesCount} — Kills: {p.TotalKills}");
                            pos++;
                        }
                        // Añadir nota si hay más jugadores
                        if (ranking.Count > top.Count)
                        {
                            sb.AppendLine($"... y {ranking.Count - top.Count} jugadores más.");
                        }
                        mensaje = sb.ToString();
                    }

                    await channel.SendMessageAsync(mensaje);
                    Console.WriteLine("📤 Mensaje enviado correctamente.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Error enviando el mensaje: " + ex);
                }
            }
            else
            {
                Console.WriteLine("❌ No se encontró el canal especificado.");
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
}
