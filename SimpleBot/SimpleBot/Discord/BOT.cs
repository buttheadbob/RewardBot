using System;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimpleBot.DiscordBot.BOT_SlashCommands;
using SimpleBot.DiscordBot.Rewards;
using SimpleBot.Settings;
using static SimpleBot.MainBot;

namespace SimpleBot.DiscordBot
{
    public sealed class Bot
    {
        private static readonly List<SocketGuildUser> Users = new List<SocketGuildUser>();
        public DiscordSocketClient Client;
        public List<SocketGuild> Guilds = new List<SocketGuild>();
        public readonly RewardBoosters BoostReward = new RewardBoosters();

        public Bot()
        {
            Task task = MainAsync();
        }

        private async Task MainAsync()
        {
            DiscordSocketConfig config = new DiscordSocketConfig()
            {
                GatewayIntents =  GatewayIntents.GuildMembers
            };
            Client = new DiscordSocketClient(config);
            Client.Log += Logging;
            Client.Connected += _client_Connected; 
            Client.Disconnected += ClientOnDisconnected;
            Client.Ready += ClientOnReady;

            await Task.Delay(-1);
        }

        private async Task ClientOnDisconnected(Exception arg)
        {
            await Client.LogoutAsync();
            await Client.StopAsync();
            await Client.DisposeAsync();
            Instance.Config.BotStatus = BotStatus.Offline;

            Log.Info($"{Instance.Config.BotName} disconnected.");
        }

        public async Task Connect()
        {
            string token = Instance.Config.BotKey;
            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();
            Instance.Config.BotStatus = BotStatus.Online;
        }

        public async Task Disconnect()
        {
            await ClientOnDisconnected(null);
        }

        private Task _client_Connected()
        {
            Guilds = new List<SocketGuild>(Client.Guilds); // Get list of all servers the bot is on.  This should only contain 1.
            Instance.Config.BotStatus = BotStatus.Online;
            
            return Task.CompletedTask;
        }

        private async Task ClientOnReady()
        {
            await GetUsersAsync(Guilds[0]);
            SocketGuildUser botUser = Guilds[0].GetUser(Client.CurrentUser.Id);
            await botUser.ModifyAsync(x => { x.Nickname = Instance.Config.BotName; });
            await Client.SetStatusAsync(UserStatus.DoNotDisturb);
            await Client.SetGameAsync(Instance.Config.BotStatusMessage, null, ActivityType.Playing);
            Client.UserJoined += _client_UserJoined;
            Client.UserLeft += _client_UserLeft;
            Client.UserBanned += ClientOnUserBanned;
            Client.SlashCommandExecuted += CommandRan.ClientOnSlashCommandExecuted;
            UserCommands userCommands = await UserCommands.CreateAsync();
        }

        private Task ClientOnUserBanned(SocketUser bannedUser, SocketGuild server)
        {
            // If option to remove banned users from registry is set.
            if (Instance.Config.RemoveBannedUsersFromRegistry)
            {
                for (int index = Instance.Config.RegisteredUsers.Count - 1; index >= 0; index--)
                {
                    RegisteredUsers registeredUser = Instance.Config.RegisteredUsers[index];
                    if (registeredUser.DiscordId == bannedUser.Id)
                    {
                        Instance.Config.RegisteredUsers.RemoveAt(index);
                    }
                }
            }
            
            // Remove all link request from banned users.
            for (int index = Instance.Config.LinkRequests.Count - 1; index >= 0; index--)
            {
                LinkRequest linkRequest = Instance.Config.LinkRequests[index];
                if (linkRequest.DiscordId == bannedUser.Id)
                {
                    Instance.Config.LinkRequests.RemoveAt(index);
                }
            }
            
            return Task.CompletedTask;
        }

        private Task _client_UserLeft(SocketGuild arg1, SocketUser arg2)
        {
            Log.Info($"{arg2.Username} has left {arg1.Name} Discord");

            for (int i = Users.Count -1; i >= 0; i--)
            {
                if (Users[i].Id != arg2.Id) continue;
                Log.Info($"{arg2.Username} subscription to {Instance.Config.BotName} has been cancelled.");
                Users.RemoveAt(i);

                Members removeMember = new Members();
                foreach (Members member in Instance.DiscordMembers)
                {
                    if (member.Username != arg2.Username) continue;
                    removeMember = member;
                    break;
                }

                if (string.IsNullOrEmpty(removeMember.Username))
                    return Task.CompletedTask;

                Instance.DiscordMembers.Remove(removeMember);


                break;
            }
            return Task.CompletedTask;
        }

        private Task _client_UserJoined(SocketGuildUser arg)
        {
            Users.Add(arg);
            Instance.DiscordMembers.Add(new Members { Nickname = arg.Nickname, Username = arg.Username, UserId = arg.Id });
            return Task.CompletedTask;
        }

        private async Task GetUsersAsync(SocketGuild guild)
        {
            await guild.DownloadUsersAsync(); // Gets all users on the first server in the list of servers.
            lock (Instance.DiscordMembersLock)
            {
                foreach (SocketGuildUser user in guild.Users)
                {
                    Users.Add(user);
                    Instance.DiscordMembers.Add(new Members{Nickname = user.Nickname, Username = user.Username, UserId = user.Id});
                }
            }
        }

        private static Task Logging(LogMessage msg)
        {
            // Convert and pass Discord logging through to Torch.
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

                default:
                    Log.Warn($"Invalid SeverityLevel from Discord Log --> {msg.Severity} :: {msg.Source} :: {msg.Message}");
                    break;
            }
            
            return Task.CompletedTask;
        }
    }
}

