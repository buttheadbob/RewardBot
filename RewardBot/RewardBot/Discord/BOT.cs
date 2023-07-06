using System;
using Discord;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Data;
using RewardBot.DiscordBot.BOT_SlashCommands;
using RewardBot.DiscordBot.Utils;
using RewardBot.Settings;
using static RewardBot.MainBot;
using AsynchronousObservableConcurrentList;
using Helper = RewardBot.DiscordBot.Utils.Helper;

namespace RewardBot.DiscordBot
{
    public sealed class Bot
    {
        public DiscordSocketClient Client;
        public List<SocketGuild> Guilds = new List<SocketGuild>();
        public SocketGuild Guild;
        public UserUtils UserUtils = new UserUtils();
        public Helper HelperUtils = new Helper();
        public List<SocketApplicationCommand> ExistingCommands = new List<SocketApplicationCommand>();
        public List<SlashCommandProperties> UserCommands = new List<SlashCommandProperties>();
        public AsynchronousObservableConcurrentList<SocketRole> Roles = new AsynchronousObservableConcurrentList<SocketRole>();
        public static object Roles_LOCK = new object();

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
            config.MaxWaitBetweenGuildAvailablesBeforeReady = 30000;
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
                Instance.Control.Dispatcher.BeginInvoke((Action)(() =>
                {
                    Instance.DiscordMembers.RemoveAt(index);
                    Instance.DiscordMembers.Add(newUserData);
                }));
                
                break;
            }
            return Task.CompletedTask;
        }

        private async Task ClientOnRoleUpdated(SocketRole originalRole, SocketRole updatedRole)
        {
            await Instance.Control.Dispatcher.BeginInvoke((Action)(() =>
            {
                Roles.Remove(originalRole);
                Roles.Add(updatedRole);
            }));
        }

        private async Task ClientOnRoleDeleted(SocketRole role)
        {
            await Instance.Control.Dispatcher.BeginInvoke((Action)(() =>
            {
                if (Roles.Contains(role))
                    Roles.Remove(role);
            }));
        }

        private async Task ClientOnRoleCreated(SocketRole role)
        {
            await Instance.Control.Dispatcher.BeginInvoke((Action)(() => { Roles.Add(role); }));
        }

        private async Task ClientOnDisconnected(Exception arg)
        {
            await Client.SetStatusAsync(UserStatus.DoNotDisturb);
            Instance.Config.SetBotStatus(BotStatusEnum.Disconnecting);
            await Client.LogoutAsync();
            await Client.StopAsync();
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
            
            return Task.CompletedTask;
        }

        private async Task VerifySingleGuild()
        {
            if (Guilds.Count != 1)
            {
                foreach (SocketGuild guild in Client.Guilds)
                {
                    await guild.DownloadUsersAsync();
                }
                
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

                await Log.Error(tooManyGuildsError);
            }
        }

        private async Task ClientOnReady()
        {
            try
            {
                Guild = Client.GetGuild(Guilds[0].Id);
                await GetRoles();
                await GetUsersAsync();
                SocketGuildUser botUser = Guild.GetUser(Client.CurrentUser.Id);
                await botUser.ModifyAsync(x => { x.Nickname = Instance.Config.BotName; });
                await Client.SetGameAsync(Instance.Config.BotStatusMessage, null, ActivityType.Playing);
                Client.UserJoined += _client_UserJoined;
                Client.UserLeft += _client_UserLeft;
                Client.UserBanned += ClientOnUserBanned;
                Client.SlashCommandExecuted += CommandRan.ClientOnSlashCommandExecuted;
                switch (Instance.WorldOnline)
                {
                    case true:
                        await Client.SetStatusAsync(UserStatus.Online);
                        break;
                    case false:
                        await Client.SetStatusAsync(UserStatus.Idle);
                        break;
                }
            
                GuildUserCommandBuilder userCommandBuilder = await GuildUserCommandBuilder.CreateAsync();
            
                Instance.Config.SetBotStatus(BotStatusEnum.Online);
                await Client.SetStatusAsync(UserStatus.Online);
                await VerifySingleGuild();
            } catch (Exception ex)
            {
                await Log.Error($"Error in ClientOnReady: {ex.Message}");
                await Log.Error($"Stack trace: {ex.StackTrace}");
            }
            
        }

        private async Task GetRoles()
        {
            try
            {
                // Shitty API fires this event before its ready so... we make the fucker wait!!
                Stopwatch maxWait = new Stopwatch();
                maxWait.Start();
                while (maxWait.ElapsedMilliseconds < 5000 )
                {
                    Thread.Sleep(100);
                }
                maxWait.Stop();

                await Log.Info("Roles Downloaded");
                
                IReadOnlyCollection<SocketRole> roles = Guild.Roles;
            
                await Instance.Control.Dispatcher.BeginInvoke((Action)(()=> {Roles.AddRange(roles);}));
                await Instance.Control.tbRoleComboBox.Dispatcher.BeginInvoke((Action)(() =>
                {
                    Instance.Control.tbRoleComboBox.ItemsSource = null;
                    Instance.Control.tbRoleComboBox.ItemsSource = MainBot.DiscordBot.Roles;
                
                }));
            } catch (Exception ex)
            {
                await Log.Error($"Error in GetRoles: {ex.Message}");
                await Log.Error($"Stack trace: {ex.StackTrace}");
            }
            
        }

        private Task ClientOnUserBanned(SocketUser bannedUser, SocketGuild server)
        {
            // If option to remove banned users from registry is set.
            if (Instance.Config.RemoveBannedUsersFromRegistry)
            {
                for (int index = 0; index < Instance.Config.RegisteredUsers.Count; index++)
                {
                    if (Instance.Config.RegisteredUsers[index].DiscordId == bannedUser.Id)
                        Application.Current.Dispatcher.BeginInvoke(new Action (() => {Instance.Config.RegisteredUsers.RemoveAt(index); }));
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

        private async Task _client_UserLeft(SocketGuild guild, SocketUser user)
        {
            await Log.Info($"{user.Username} has left {guild.Name} Discord");

            for (int i = Instance.DiscordMembers.Count -1; i >= 0; i--)
            {
                if (Instance.DiscordMembers[i].Id != user.Id) continue;
                await Log.Info($"{user.Username} subscription to {Instance.Config.BotName} has been cancelled.");
                Instance.DiscordMembers.RemoveAt(i);

                SocketGuildUser removeMember = null;
                for (int index = Instance.DiscordMembers.Count - 1; index >= 0; index--)
                {
                    SocketGuildUser member = Instance.DiscordMembers[index];
                    if (member.Username != user.Username) continue;
                    removeMember = member;
                    break;
                }

                if (removeMember == null) return;
                
                if (string.IsNullOrEmpty(removeMember.Username)) return;

                Instance.DiscordMembers.Remove(removeMember);
                break;
            }
        }

        private  Task _client_UserJoined(SocketGuildUser user)
        {
            Instance.DiscordMembers.Add(user);
            return Task.CompletedTask;
        }

        private async Task GetUsersAsync()
        {
            await Guild.DownloadUsersAsync(); // Gets all users on the first server in the list of servers.
            await Instance.Control.Dispatcher.BeginInvoke((Action) (() => { Instance.DiscordMembers.AddRange(Guild.Users); }));
        }

        private static async Task Logging(LogMessage msg)
        {
            if (msg.Message == null)
            {
                await Log.Error(msg.ToString());
                return;
            }

            if (msg.Message.Contains("Discord.Net v3.10.0 (API v10)"))
                return; // Don't need this spammed in logs on every restart.
            
            if (msg.ToString().Contains("Discord.Net.HttpException: The server responded with error 401"))
            {
                await Log.Error("Discord responded with error 401:Unauthorized.  Is your Token/Key valid?");
                return;
            }

            if (msg.Exception is TaskCanceledException)
            {
                // Don't report this, task cancellations is used when the bot goes offline.
                return;
            }
            
            // Convert and pass Discord logging through to Torch.
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    await Log.Error(msg.ToString());
                    break;

                case LogSeverity.Debug:
                    await Log.Debug(msg.ToString());
                    break;

                case LogSeverity.Warning:
                    await Log.Warn(msg.ToString());
                    break;

                case LogSeverity.Info:
                case LogSeverity.Verbose:
                    await Log.Info(msg.ToString());
                    break;

                default:
                    await Log.Warn($"Invalid SeverityLevel from Discord Log --> {msg.Severity} :: {msg.Source} :: {msg.Message}");
                    break;
            }
        }
    }
}

