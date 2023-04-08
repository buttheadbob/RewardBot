using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Discord.WebSocket;
using NLog;
using RewardBot.Settings;
using static RewardBot.MainBot;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;

namespace RewardBot.Utils
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

    public sealed class ID_Manager
    {
        private static object lastIDLOCK = new object();
        private static object lastRewardLOCK = new object();

        public int GetNewPayoutID()
        {
            lock (lastIDLOCK)
            {
                return Instance.Config.LastPayoutId++;
            }
        }

        public int GetLastPayoutID()
        {
            lock (lastIDLOCK)
            {
                return Instance.Config.LastPayoutId;
            }
        }

        public int GetNewRewardID()
        {
            lock (lastRewardLOCK)
            {
                return Instance.Config.LastRewardId++;
            }
        }

        public int GetLastRewardID()
        {
            lock (lastRewardLOCK)
            {
                return Instance.Config.LastRewardId;
            }
        }
    }

    public sealed class PayManager
    {
        /// <summary>
        /// Runs through all the Rewards setup and checks if any registered user qualifies to receive it.
        /// </summary>
        /// <param name="payAll">False: Only qualifying registered users will receive rewards.  True: ALL registered users will receive rewards.</param>
        public async Task Payout(int rewardID = 0, bool payAll = false)
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
                                Settings.Payout payout = new Payout()
                                {
                                    ID = MainBot.IdManager.GetNewPayoutID(),
                                    Name = reward.Name,
                                    IngameName = registeredUser.IngameName,
                                    SteamID = registeredUser.IngameSteamId,
                                    PaymentDate = DateTime.Now,
                                    ExpiryDate = DateTime.Now + TimeSpan.FromDays(reward.ExpiresInDays),
                                    Command = finishCommand,
                                    DiscordId = registeredUser.DiscordId,
                                    DiscordName = registeredUser.DiscordUsername
                                };
                                Instance.Config.Payouts.Add(payout);
                                registeredUser.LastPayout = DateTime.Now;
                                break;
                            }
                        }
                    }
                    reward.LastRun = DateTime.Now;
                }
                await Instance.Save();
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
                            Payout payout = new Payout()
                            {
                                ID = IdManager.GetNewPayoutID(),
                                Name = reward.Name,
                                IngameName = registeredUser.IngameName,
                                SteamID = registeredUser.IngameSteamId,
                                PaymentDate = DateTime.Now,
                                ExpiryDate = DateTime.Now + TimeSpan.FromDays(reward.ExpiresInDays),
                                Command = finishCommand,
                                DiscordId = registeredUser.DiscordId,
                                DiscordName = registeredUser.DiscordUsername
                            };
                            Instance.Config.Payouts.Add(payout);
                            StringBuilder logReward = new StringBuilder();
                            logReward.AppendLine("REWARD ISSUED!");
                            logReward.AppendLine($"ID           -> {payout.ID}");
                            logReward.AppendLine($"Reward       -> {payout.Name}");
                            logReward.AppendLine($"In-Game Name -> {payout.IngameName}");
                            logReward.AppendLine($"SteamID      -> {payout.SteamID}");
                            logReward.AppendLine($"Discord Name -> {payout.DiscordName}");
                            logReward.AppendLine($"Command      -> {payout.Command}");
                            logReward.AppendLine($"Expires      -> [{payout.DaysUntilExpired} days]  {payout.ExpiryDate}");
                            logReward.AppendLine($"---------------------------------------");
                            await MainBot.Log.Warn(logReward);
                        }
                    }
                }

            }
            await Instance.Save();
        }

        public async Task ManualPayout(ulong steamId, string command, int expiresInDays)
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
                
                Instance.Config.Payouts.Add(manualReward);
                await Instance.Save();

            }

            if (!foundUser)
                MessageBox.Show($"Unable to locate a registered user with SteamID {steamId}.", "Check Your SteamID",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    
    /// <summary>
        /// NLog manager for asynchronous, thread safe logging.  This works on an user set timed queued system to write logs. 
        /// </summary>
        public sealed class Log : IDisposable
        {
            private static Logger _log;
            private static readonly Timer AsyncBatchProcess = new Timer();
            private static Queue<Tuple<string,string, DateTime>> _queue = new Queue<Tuple<string, string, DateTime>>();
            private static readonly object QueueLock = new object();

            /// <summary>
            /// Created a log safely and allows fast burst of application logging without system bog.
            /// </summary>
            /// <param name="logger">OPTIONAL: Pass in an existing Nlog instance to be used instead.</param>
            public Log(ref Logger logger)
            {
                _log = logger;
                
                AsyncBatchProcess.Interval = 5000;
                AsyncBatchProcess.Elapsed += AsyncBatchProcessOnElapsed;
                AsyncBatchProcess.Enabled = true;
                AsyncBatchProcess.Start();
            }

            /// <summary>
            /// Pause writing the queue to log file.  Useful if you know you will write a large amount of data in a very short period of time.
            /// </summary>
            /// <param name="wait">Time in seconds, of type double.</param>
            public Task Pause(int wait)
            {
                Stopwatch waiter = new Stopwatch();
                AsyncBatchProcess.Stop();
                waiter.Start();
                while (waiter.Elapsed.Seconds < wait)
                {
                    
                }
                AsyncBatchProcess.Start();
                return Task.CompletedTask;
            }

            /// <summary>
            /// Stops writing the queued logs to file.  Useful if you want to write a large amount of data and reactivate log after.
            /// </summary>
            public void Stop()
            {
                AsyncBatchProcess.Stop();
            }

            /// <summary>
            /// Starts writing the queued logs to file.  This is by default already started.
            /// </summary>
            public void Start()
            {
                AsyncBatchProcess.Start();
            }

            /// <summary>
            /// Forces all queued logs to be written synchronously.
            /// </summary>
            public  void Flush()
            {
                ClearQueue();
            }

            /// <summary>
            ///  Forces all queued logs to be written asynchronously.  Does not require await.
            /// </summary>
            public async Task FlushAsync()
            {
                await ClearQueue();
            }
            
            /// <summary>
            /// Controls how frequently the queue system will write to the log.
            /// </summary>
            /// <param name="interval">Default is 5000 (5 seconds).  Accepts 'double' value.</param>
            public void SetQueueRate(double interval) { AsyncBatchProcess.Interval = interval; }

            private async void AsyncBatchProcessOnElapsed(object sender, ElapsedEventArgs e)
            {
                await ClearQueue();
            }

            private Task ClearQueue()
            {
                lock (QueueLock)
                {
                    int _processed = 0;
                    while (_queue.Count > 0)
                    {
                        _processed++;
                        Tuple<string, string, DateTime> value = _queue.Dequeue();
                        switch (value.Item1)
                        {
                            case "Info":
                                _log.Info($"{value.Item3.Hour:D2}.{value.Item3.Minute:D2}.{value.Item3.Second}.{value.Item3.Millisecond}    {value.Item2}");
                                break;
                            case "Debug":
                                _log.Debug($"{value.Item3.Hour:D2}.{value.Item3.Minute:D2}.{value.Item3.Second}.{value.Item3.Millisecond}    {value.Item2}");
                                break;
                            case "Warn":
                                _log.Warn($"{value.Item3.Hour:D2}.{value.Item3.Minute:D2}.{value.Item3.Second}.{value.Item3.Millisecond}    {value.Item2}");
                                break;
                            case "Error":
                                _log.Error($"{value.Item3.Hour:D2}.{value.Item3.Minute:D2}.{value.Item3.Second}.{value.Item3.Millisecond}    {value.Item2}");
                                break;
                            case "Trace":
                                _log.Trace($"{value.Item3.Hour:D2}.{value.Item3.Minute:D2}.{value.Item3.Second}.{value.Item3.Millisecond}    {value.Item2}");
                                break;
                            case "Fatal":
                                _log.Fatal($"{value.Item3.Hour:D2}.{value.Item3.Minute:D2}.{value.Item3.Second}.{value.Item3.Millisecond}    {value.Item2}");
                                break;
                            default:
                                _log.Error($"{value.Item3.Hour:D2}.{value.Item3.Minute:D2}.{value.Item3.Second}.{value.Item3.Millisecond}    {value.Item2}");
                                break;
                        }

                        if (_processed >= 50)
                        {
                            _log.Warn("Excessive Logging Detected.... Waiting.");
                            break; // Prevents long queue spams from killing torch.
                        }
                              
                    }
                }
                return Task.CompletedTask;
            }
          
            private Task _info(string value)
            {
                lock (QueueLock)
                {
                    _queue.Enqueue(new Tuple<string, string, DateTime>("Info",value,DateTime.Now));
                }
                return Task.CompletedTask;
            }
            private Task _debug(string value)
            {
                lock (QueueLock)
                {
                    _queue.Enqueue(new Tuple<string, string, DateTime>("Info",value,DateTime.Now));
                }
                return Task.CompletedTask;
            }
            private Task _warn(string value)
            {
                lock (QueueLock)
                {
                    _queue.Enqueue(new Tuple<string, string, DateTime>("Info",value,DateTime.Now));
                }
                return Task.CompletedTask;
            }
            private Task _error(string value)
            {
                lock (QueueLock)
                {
                    _queue.Enqueue(new Tuple<string, string, DateTime>("Info",value,DateTime.Now));
                }
                _log.Error(value);
                return Task.CompletedTask;
            }
            private Task _trace(string value)
            {
                lock (QueueLock)
                {
                    _queue.Enqueue(new Tuple<string, string, DateTime>("Info",value,DateTime.Now));
                }
                return Task.CompletedTask;
            }
            private Task _fatal(string value)
            {
                lock (QueueLock)
                {
                    _queue.Enqueue(new Tuple<string, string, DateTime>("Info",value,DateTime.Now));
                }
                return Task.CompletedTask;
            }

            /// <summary>
            /// Writes the diagnostic message and exception at the Info level.
            /// </summary>
            /// <param name="message">String format</param>
            public async Task Info(string message)
            {
                await _info(message);
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Info level.
            /// </summary>
            /// <param name="message">StringBuilder format</param>
            public async Task Info(StringBuilder message)
            {
                await _info(message.ToString());
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Debug level.
            /// </summary>
            /// <param name="message">String format</param>
            public async Task Debug(string message)
            {
                await _debug(message);
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Debug level.
            /// </summary>
            /// <param name="message">StringBuilder format</param>
            public async Task Debug(StringBuilder message)
            {
                await _debug(message.ToString());
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Warn level.
            /// </summary>
            /// <param name="message">String format</param>
            public async Task Warn(string message)
            {
                await _warn(message);
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Warn level.
            /// </summary>
            /// <param name="message">StringBuilder format</param>
            public async Task Warn(StringBuilder message)
            {
                await _warn(message.ToString());
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Error level.
            /// </summary>
            /// <param name="message">String format</param>
            public async Task Error(string message)
            {
                await _error(message);
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Error level.
            /// </summary>
            /// <param name="message">StringBuilder format</param>
            public async Task Error(StringBuilder message)
            {
                await _error(message.ToString());
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Trace level.
            /// </summary>
            /// <param name="message">String format</param>
            public async Task Trace(string message)
            {
                await _trace(message);
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Trace level.
            /// </summary>
            /// <param name="message">StringBuilder format</param>
            public async Task Trace(StringBuilder message)
            {
                await _trace(message.ToString());
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Fatal level.
            /// </summary>
            /// <param name="message">String format</param>
            public async Task Fatal(string message)
            {
                await _fatal(message);
            }
            /// <summary>
            /// Writes the diagnostic message and exception at the Fatal level.
            /// </summary>
            /// <param name="message">StringBuilder format</param>
            public async Task Fatal(StringBuilder message)
            {
                await _fatal(message.ToString());
            }
            /// <summary>
            /// Disposes of all disposable items. 
            /// </summary>
            public void Dispose()
            {
                AsyncBatchProcess.Stop();
                AsyncBatchProcess.Elapsed -= AsyncBatchProcessOnElapsed;
                AsyncBatchProcess.Dispose();
            }
        }

    
}