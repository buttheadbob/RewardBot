using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace DiscordRoleExample
{
    public sealed class BOT
    {
        static void Main(string[] args) => new BOT().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Log;

            string token = UD_SimpleBot.SimpleBot.Instance.Config.Bot_Key;
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            ulong guildId = 1234567890; // Replace with your own guild ID
            ulong userId = 1234567890; // Replace with the user ID you want to get roles for

            var guild = _client.GetGuild(guildId);
            var user = guild.GetUser(userId);

            if (user != null)
            {
                Console.WriteLine($"Roles for user {user.Username}:");
                foreach (var role in user.Roles)
                {
                    Console.WriteLine(role.Name);
                }
            }
            else
            {
                Console.WriteLine("User not found.");
            }

            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}

