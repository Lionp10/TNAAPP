using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        bool runOnce = args.Contains("--run-once");

        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        var channelIdString = Environment.GetEnvironmentVariable("DISCORD_CHANNEL_ID");

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

        var config = new DiscordSocketConfig
        {
            // Necesitamos Guilds y GuildMessages para que pueda acceder a los canales
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
        };

        var client = new DiscordSocketClient(config);

        client.Log += msg =>
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        };

        // TaskCompletionSource para sincronizar cuando queremos ejecutar solo una vez.
        TaskCompletionSource<bool>? readyTcs = runOnce ? new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously) : null;

        client.Ready += async () =>
        {
            Console.WriteLine($"✅ {client.CurrentUser} conectado a Discord.");

            // Esperar un poco para que los canales estén disponibles en caché
            await Task.Delay(2000);

            var channel = client.GetChannel(channelId) as IMessageChannel;
            if (channel != null)
            {
                try
                {
                    string mensaje = $"📢 Reporte diario generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC";
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

            // Señalamos que ya terminó la operación cuando estamos en modo run-once.
            if (readyTcs != null)
            {
                readyTcs.TrySetResult(true);
            }
        };

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        if (runOnce && readyTcs != null)
        {
            // Esperar a que el Ready complete (timeout prudente para evitar colgar indefinidamente)
            try
            {
                await readyTcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (TimeoutException)
            {
                Console.WriteLine("❌ Timeout esperando al evento Ready.");
            }

            // Desconectamos limpiamente
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
            await Task.Delay(-1); // Mantener vivo si no es modo run-once
    }
}
