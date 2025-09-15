using Discord;
using Discord.WebSocket;

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
            GatewayIntents = GatewayIntents.Guilds // solo lo mínimo
        };

        var client = new DiscordSocketClient(config);

        client.Log += msg =>
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        };

        client.Ready += async () =>
        {
            Console.WriteLine($"✅ {client.CurrentUser} conectado a Discord.");

            var channel = client.GetChannel(channelId) as IMessageChannel;
            if (channel != null)
            {
                // 🔹 Aquí ponés el mensaje real que quieras enviar
                string mensaje = $"📢 Reporte diario generado: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC";

                await channel.SendMessageAsync(mensaje);
                Console.WriteLine("📤 Mensaje enviado correctamente.");
            }
            else
            {
                Console.WriteLine("❌ No se encontró el canal especificado.");
            }

            if (runOnce)
            {
                await client.LogoutAsync();
                await client.StopAsync();
                Environment.Exit(0); // Finaliza la app
            }
        };

        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        // Mantener activo si no es modo --run-once
        if (!runOnce)
            await Task.Delay(-1);
    }
}