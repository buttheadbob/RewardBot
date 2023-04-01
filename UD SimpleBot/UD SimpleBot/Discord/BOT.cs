using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using static UD_SimpleBot.SimpleBot;

namespace UD_SimpleBot.DiscordBot
{
    public sealed class BOT
    {
        static void Main(string[] args) => new BOT().MainAsync().GetAwaiter().GetResult();
        static List<SocketGuildUser> users = new List<SocketGuildUser>();

        private DiscordSocketClient _client;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();

            _client.Log += Logging;

            string token = Instance.Config.Bot_Key;
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.Connected += _client_Connected;            

            await Task.Delay(-1);
        }

        private async Task _client_Connected()
        {
            List<SocketGuild> Guilds = new List<SocketGuild>(_client.Guilds); // Get list of all servers the bot is on.  This should only contain 1.
            await GetUsersAsync(Guilds[0]);
            SocketGuildUser bot_user = Guilds[0].GetUser(_client.CurrentUser.Id);
            await bot_user.ModifyAsync(x => { x.Nickname = Instance.Config.Bot_Name; });
            await _client.SetStatusAsync(UserStatus.DoNotDisturb);
            await _client.SetGameAsync("Space Engineers on UpsideDown", null, ActivityType.Playing);

            _client.UserJoined += _client_UserJoined;
            _client.UserLeft += _client_UserLeft;
        }

        public Task RewardBoosters(SocketGuild Guild)
        {
            // Reward only registered users
            if (Instance.Config.Only_Reward_Registered_Boosters)
            {
                foreach (Settings.RegisteredUsers user in Instance.Config.RegisteredUsers)
                {
                    SocketGuildUser _User = Guild.GetUser(user.Discord_ID);
                    if (_User == null)
                    {
                        Log.Info($"Registered user {_User.Username} was not located in the Discord userlist.  Unable to payout boost rewards.");
                        continue;
                    }

                    

                }
            }

            return Task.CompletedTask;            
        }

        private Task _client_UserLeft(SocketGuild arg1, SocketUser arg2)
        {
            Log.Info($"{arg2.Username} has left {arg1.Name} Discord");

            for (int i = users.Count -1; i >= 0; i--)
            {
                if (users[i].Id == arg2.Id)
                {
                    Log.Info($"{arg2.Username} subscription to {Instance.Config.Bot_Name} has been cancelled.");
                    users.RemoveAt(i);
                    break;
                }
            }
            return Task.CompletedTask;
        }

        private Task _client_UserJoined(SocketGuildUser arg)
        {
            users.Add(arg);
            return Task.CompletedTask;
        }


        private async Task GetUsersAsync(SocketGuild Guild)
        {
            await Guild.DownloadUsersAsync(); // Gets all users on the first server in the list of servers.
            foreach (SocketGuildUser _user in Guild.Users)
                users.Add(_user);
        }

        private Task Logging(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Log.Error(msg.ToString());
                    break;

                case LogSeverity.Debug:
                    Log.Debug(msg.ToString());
                    break;

                case LogSeverity.Warning:
                    Log.Warn(msg.ToString());
                    break;

                case LogSeverity.Info:
                case LogSeverity.Verbose:
                    Log.Info(msg.ToString());
                    break;
            }
            
            return Task.CompletedTask;
        }
    }
}

