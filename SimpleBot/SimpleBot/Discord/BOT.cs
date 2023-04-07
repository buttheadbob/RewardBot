using System;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using SimpleBot.DiscordBot.BOT_SlashCommands;
using SimpleBot.DiscordBot.Utils;
using SimpleBot.Settings;
using SimpleBot.Utils;
using static SimpleBot.MainBot;
using AsynchronousObservableConcurrentList;
using Helper = SimpleBot.DiscordBot.Utils.Helper;

namespace SimpleBot.DiscordBot
{
    public sealed class Bot
    {
        public DiscordSocketClient Client;
        public List<SocketGuild> Guilds = new List<SocketGuild>();
        public UserUtils UserUtils = new UserUtils();
        public Helper HelperUtils = new Helper();
        public List<SocketApplicationCommand> ExistingCommands = new List<SocketApplicationCommand>();
        public List<SlashCommandProperties> AllCommands = new List<SlashCommandProperties>();
        public AsynchronousObservableConcurrentList<SocketRole> Roles = new AsynchronousObservableConcurrentList<SocketRole>();
        public static object Roles_LOCK = new object();
        public PayManager RewardManager = new PayManager();
        

        public Bot()
        {
            Task task = MainAsync();
        }

        private async Task MainAsync()
        {
            BindingOperations.EnableCollectionSynchronization(Roles , Roles_LOCK);
            DiscordSocketConfig config = new DiscordSocketConfig()
            {
                GatewayIntents = GatewayIntents.Guilds 
                                 | GatewayIntents.GuildMembers
                                 | GatewayIntents.GuildBans
            };
            Client = new DiscordSocketClient(config);
            Client.Log += Logging;
            Client.Connected += _client_Connected; 
            Client.Disconnected += ClientOnDisconnected;
            Client.Ready += ClientOnReady;
            Client.UserBanned += ClientOnUserBanned;
            Client.RoleCreated += ClientOnRoleCreated;
            Client.RoleDeleted += ClientOnRoleDeleted;
            Client.RoleUpdated += ClientOnRoleUpdated;
            Client.GuildMemberUpdated += ClientOnGuildMemberUpdated;

            await Task.Delay(-1);
        }

        private Task ClientOnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> oldUserData, SocketGuildUser newUserData)
        {
            for (int index = Instance.DiscordMembers.Count - 1; index >= 0; index--)
            {
                if (Instance.DiscordMembers[index].Id != oldUserData.Id) continue;
                Instance.DiscordMembers.RemoveAt(index);
                Instance.DiscordMembers.Add(newUserData);
                break;
            }
            return Task.CompletedTask;
        }

        private Task ClientOnRoleUpdated(SocketRole originalRole, SocketRole updatedRole)
        {
            Instance.Control.Dispatcher.BeginInvoke((Action)(() =>
            {
                Roles.Remove(originalRole);
                Roles.Add(updatedRole);
            }));
            return Task.CompletedTask;
        }

        private Task ClientOnRoleDeleted(SocketRole role)
        {
            Instance.Control.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (Roles.Contains(role))
                    Roles.Remove(role);
            }));
            return Task.CompletedTask;
        }

        private Task ClientOnRoleCreated(SocketRole role)
        {
            Instance.Control.Dispatcher.BeginInvoke((Action)(() => { Roles.Add(role); }));
            return Task.CompletedTask;
        }

        private async Task ClientOnDisconnected(Exception arg)
        {
            Instance.Config.SetBotStatus(BotStatusEnum.Disconnecting);
            await Client.StopAsync();
            await Client.LogoutAsync();
            Instance.Config.SetBotStatus(BotStatusEnum.Offline);
        }

        public async Task Connect()
        {
            await Client.LoginAsync(TokenType.Bot, Instance.Config.BotKey);
            await Client.StartAsync();
            Instance.Config.SetBotStatus(BotStatusEnum.Connecting);
        }

        public async Task Disconnect()
        {
            await ClientOnDisconnected(null);
        }

        private Task _client_Connected()
        {
            Guilds = new List<SocketGuild>(Client.Guilds); // Get list of all servers the bot is on.  This should only contain 1.
            if (Guilds.Count != 1)
            {
                StringBuilder tooManyGuildsError = new StringBuilder();
                tooManyGuildsError.AppendLine("This bot is registered on more than 1 Discord server.");
                tooManyGuildsError.AppendLine("| List of Servers |");
                for (int index = Guilds.Count - 1; index >= 0; index--)
                {
                    SocketGuild guild = Guilds[index];
                    tooManyGuildsError.AppendLine($"{index + 1} -> Name:{guild.Name}");
                    tooManyGuildsError.AppendLine($"{index + 1} -> Server ID:{guild.Id}");
                    tooManyGuildsError.AppendLine($"{index + 1} -> Owner Username:{guild.Owner.Username}");
                    if (!string.IsNullOrEmpty(guild.Owner.Nickname))
                        tooManyGuildsError.AppendLine($"{index + 1} -> Owner Nickname:{guild.Owner.Nickname}");
                    tooManyGuildsError.AppendLine($"{index + 1} -> Owner ID:{guild.Owner.Id}");
                    tooManyGuildsError.AppendLine("---------------------------------------");
                }
                tooManyGuildsError.AppendLine("** THIS BOT SHOULD ONLY BE REGISTERED ON 1 DISCORD SERVER, UNEXPECTED RESULTS MAY OCCUR **");

                Log.Error(tooManyGuildsError);
            }
            Instance.Config.SetBotStatus(BotStatusEnum.Online);
            
            return Task.CompletedTask;
        }

        private async Task ClientOnReady()
        {
            await GetRoles(Guilds[0]);
            await GetUsersAsync(Guilds[0]);
            SocketGuildUser botUser = Guilds[0].GetUser(Client.CurrentUser.Id);
            await botUser.ModifyAsync(x => { x.Nickname = Instance.Config.BotName; });
            await Client.SetStatusAsync(UserStatus.DoNotDisturb);
            await Client.SetGameAsync(Instance.Config.BotStatusMessage, null, ActivityType.Playing);
            Client.UserJoined += _client_UserJoined;
            Client.UserLeft += _client_UserLeft;
            Client.UserBanned += ClientOnUserBanned;
            Client.SlashCommandExecuted += CommandRan.ClientOnSlashCommandExecuted;
            Instance.Config.SetBotStatus(BotStatusEnum.Online);
        }

        private Task GetRoles(SocketGuild guild)
        {
            IReadOnlyCollection<SocketRole> roles = guild.Roles;
            Instance.Control.Dispatcher.BeginInvoke((Action)(()=> {Roles.AddRange(roles);}));
            return Task.CompletedTask;
        }

        private Task ClientOnUserBanned(SocketUser bannedUser, SocketGuild server)
        {
            // If option to remove banned users from registry is set.
            if (Instance.Config.RemoveBannedUsersFromRegistry)
            {
                for (int index = 0; index < Instance.Config.RegisteredUsers.Count; index++)
                {
                    if (Instance.Config.RegisteredUsers[index].DiscordId == bannedUser.Id)
                        Instance.Config.RegisteredUsers.RemoveAt(index);
                }
            }
            
            // Remove all link request from banned users.
            for (int index = Instance.Config.LinkRequests.Count - 1; index >= 0; index--)
            {
                LinkRequest linkRequest = Instance.Config.LinkRequests[index];
                if (linkRequest.DiscordId == bannedUser.Id)
                    Instance.Config.LinkRequests.RemoveAt(index);
            }
            
            return Task.CompletedTask;
        }

        private Task _client_UserLeft(SocketGuild guild, SocketUser user)
        {
            Log.Info($"{user.Username} has left {guild.Name} Discord");

            for (int i = Instance.DiscordMembers.Count -1; i >= 0; i--)
            {
                if (Instance.DiscordMembers[i].Id != user.Id) continue;
                Log.Info($"{user.Username} subscription to {Instance.Config.BotName} has been cancelled.");
                Instance.DiscordMembers.RemoveAt(i);

                SocketGuildUser removeMember = null;
                for (int index = Instance.DiscordMembers.Count - 1; index >= 0; index--)
                {
                    SocketGuildUser member = Instance.DiscordMembers[index];
                    if (member.Username != user.Username) continue;
                    removeMember = member;
                    break;
                }

                if (removeMember == null)
                    return Task.CompletedTask;
                
                if (string.IsNullOrEmpty(removeMember.Username))
                    return Task.CompletedTask;

                Instance.DiscordMembers.Remove(removeMember);
                break;
            }
            return Task.CompletedTask;
        }

        private Task _client_UserJoined(SocketGuildUser user)
        {
            Instance.DiscordMembers.Add(user);
            return Task.CompletedTask;
        }

        private async Task GetUsersAsync(SocketGuild guild)
        {
            await guild.DownloadUsersAsync(); // Gets all users on the first server in the list of servers.
            Instance.DiscordMembers.AddRange(guild.Users);
        }

        private static Task Logging(LogMessage msg)
        {
            if (msg.Message == null)
            {
                Log.Error(msg.ToString);
                return Task.CompletedTask;
            }

            if (msg.Message.Contains("Discord.Net v3.10.0 (API v10)"))
                return Task.CompletedTask; // Don't need this spammed in logs on every restart.
            
            if (msg.ToString().Contains("Discord.Net.HttpException: The server responded with error 401"))
            {
                Log.Error("Discord responded with error 401:Unauthorized.  Is your Token/Key valid?");
                return Task.CompletedTask;
            }

            if (msg.Message.Contains("A task was canceled"))
            {
                // Don't report this, task cancellations is used when the bot goes offline.
                return Task.CompletedTask;
            }
            
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

