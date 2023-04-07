using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Discord.WebSocket;
using SimpleBot.Settings;
using static SimpleBot.MainBot;
using MessageBox = System.Windows.MessageBox;

namespace SimpleBot.Utils
{
    public static class Helper
    {
        private static readonly object RegisteredUserLock = new object(); 
        
        // SunsetQuest Method
        public static string RemoveWhiteSpace(string str)
        {
            int len = str.Length;
            char[] src = str.ToCharArray();
            int dstIdx = 0;

            for (int i = 0; i < len; i++) {
                char ch = src[i];

                switch (ch) {

                    case '\u0020': case '\u00A0': case '\u1680': case '\u2000': case '\u2001':
                    case '\u2002': case '\u2003': case '\u2004': case '\u2005': case '\u2006':
                    case '\u2007': case '\u2008': case '\u2009': case '\u200A': case '\u202F':
                    case '\u205F': case '\u3000': case '\u2028': case '\u2029': case '\u0009':
                    case '\u000A': case '\u000B': case '\u000C': case '\u000D': case '\u0085':
                        continue;

                    default:
                        src[dstIdx++] = ch;
                        break;
                }
            }

            return new string(src, 0, dstIdx);
        }
    }

    public sealed class PayManager
    {
        /// <summary>
        /// Runs through all the Rewards setup and checks if any registered user qualifies to receive it.
        /// </summary>
        /// <param name="payAll">False: Only qualifying registered users will receive rewards.  True: ALL registered users will receive rewards.</param>
        public async Task Payout(bool payAll = false)
        {
            // Make sure user data is up to date!
            await MainBot.DiscordBot.Guilds[0].DownloadUsersAsync();
            IReadOnlyCollection<SocketGuildUser> guildUsers = MainBot.DiscordBot.Guilds[0].Users;
            
            if (!payAll)
            {
                for (int rewardIndex = Instance.Config.Rewards.Count - 1; rewardIndex >= 0; rewardIndex--)
                {
                    Reward reward = Instance.Config.Rewards[rewardIndex];
                    for (int userIndex = Instance.Config.RegisteredUsers.Count - 1; userIndex >= 0; userIndex--)
                    {
                        RegisteredUsers registeredUser = Instance.Config.RegisteredUsers[userIndex];
                        if (reward.LastRun.Day == DateTime.Now.Day) continue;

                        foreach (SocketGuildUser socketGuildUser in guildUsers)
                        {
                            if (socketGuildUser.Id != registeredUser.DiscordId) continue;
                            IReadOnlyCollection<SocketRole> roles = socketGuildUser.Roles;
                            foreach (SocketRole role in roles)
                            {
                                if (role.Name != reward.CommandRole) continue;
                                string[] paydays = reward.DaysToPay.Split(',');
                                bool paydayToday = false;
                                for (int i = paydays.Length - 1; i >= 0; i--)
                                {
                                    if (paydays[i] == DateTime.Now.Day.ToString())
                                        paydayToday = true;
                                }
                                if (!paydayToday) continue;
                        
                                string startCommand = reward.Command.Replace("{SteamID}", registeredUser.IngameSteamId.ToString());
                                string finishCommand = startCommand.Replace("{Username}", registeredUser.IngameName);
                                Instance.Config.Payouts.Add(new Payout()
                                {
                                    IngameName = registeredUser.IngameName,
                                    SteamID = registeredUser.IngameSteamId,
                                    PaymentDate = DateTime.Now,
                                    ExpiryDate = DateTime.Now + TimeSpan.FromDays(reward.ExpiresInDays),
                                    Command = finishCommand,
                                    DiscordId = registeredUser.DiscordId,
                                    DiscordName = registeredUser.DiscordUsername
                                });
                                registeredUser.LastPayout = DateTime.Now;
                                break;
                            }
                        }
                    }
                    reward.LastRun = DateTime.Now;
                }
                Instance.Save();
                return;
            }
            
            for (int index = Instance.Config.RegisteredUsers.Count - 1; index >= 0; index--)
            {
                RegisteredUsers registeredUser = Instance.Config.RegisteredUsers[index];

                for (int rewardIndex = Instance.Config.Rewards.Count - 1; rewardIndex >= 0; rewardIndex--)
                {
                    Reward reward = Instance.Config.Rewards[rewardIndex];
                    
                    foreach (SocketGuildUser socketGuildUser in guildUsers)
                    {
                        if (socketGuildUser.Id != registeredUser.DiscordId) continue;
                        IReadOnlyCollection<SocketRole> roles = socketGuildUser.Roles;
                        foreach (SocketRole role in roles)
                        {
                            if (role.Name != reward.CommandRole) continue;
                            string startCommand = reward.Command.Replace("{SteamID}", registeredUser.IngameSteamId.ToString());
                            string finishCommand = startCommand.Replace("{Username}", registeredUser.IngameName);
                            Instance.Config.Payouts.Add(new Payout()
                            {
                                IngameName = registeredUser.IngameName,
                                SteamID = registeredUser.IngameSteamId,
                                PaymentDate = DateTime.Now,
                                ExpiryDate = DateTime.Now + TimeSpan.FromDays(reward.ExpiresInDays),
                                Command = finishCommand,
                                DiscordId = registeredUser.DiscordId,
                                DiscordName = registeredUser.DiscordUsername
                            });
                        }
                    }
                }

            }
            Instance.Save();
        }

        public Task ManualPayout(ulong steamId, string command, int expiresInDays)
        {
            bool foundUser = false;
            for (int i = Instance.Config.RegisteredUsers.Count - 1; i >= 0; i--)
            {
                if (Instance.Config.RegisteredUsers[i].IngameSteamId != steamId) continue;

                foundUser = true;
                string startCommand = command.Replace("{Username}", Instance.Config.RegisteredUsers[i].IngameName);
                string finishCommand = startCommand.Replace("{SteamID}", steamId.ToString());
                Payout manualReward = new Payout()
                {
                    SteamID = steamId,
                    DiscordId = Instance.Config.RegisteredUsers[i].DiscordId,
                    DiscordName = Instance.Config.RegisteredUsers[i].DiscordUsername,
                    IngameName = Instance.Config.RegisteredUsers[i].IngameName,
                    ExpiryDate = DateTime.Now + TimeSpan.FromDays(expiresInDays),
                    Command = finishCommand
                };
                return Task.CompletedTask;

            }
            if (!foundUser) 
                MessageBox.Show($"Unable to locate a registered user with SteamID {steamId}.","Check Your SteamID",MessageBoxButton.OK,MessageBoxImage.Error);
            return Task.CompletedTask;
        }
    }
}

namespace SenXThreadSafeDataGrid
{
    public class ThreadSafeDataGrid<T> : DataGridView
    {
        private readonly SynchronizationContext _syncContext;

        public ThreadSafeDataGrid()
        {
            _syncContext = SynchronizationContext.Current;
            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AutoGenerateColumns = true;
            DataSource = new List<T>();
        }

        public void UpdateDataSource(IEnumerable<T> source)
        {
            _syncContext.Post(_ => {
                DataSource = source;
            }, null);
        }

        public void AddRow(T item)
        {
            _syncContext.Post(_ => {
                var source = DataSource as List<T>;
                source.Add(item);
                DataSource = null;
                DataSource = source;
            }, null);
        }

        public void RemoveRow(T item)
        {
            _syncContext.Post(_ => {
                var source = DataSource as List<T>;
                source.Remove(item);
                DataSource = null;
                DataSource = source;
            }, null);
        }
    }
}