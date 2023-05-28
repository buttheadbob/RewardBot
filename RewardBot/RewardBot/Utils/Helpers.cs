using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using Discord;
using Discord.WebSocket;
using NLog;
using RewardBot.Settings;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Torch.Mod;
using Torch.Mod.Messages;
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
            if (!Instance.Config.IsBotOnline())
            {
                await MainBot.Log.Warn("Unable to process rewards while the Discord bot is offline.");
                return;
            }
            
            // Grab online players to display announcements.
            List<MyPlayer> _onlineUsers = new List<MyPlayer>();
            if(Instance.WorldOnline)
                _onlineUsers = Sync.Players.GetOnlinePlayers().ToList();
            
            Dictionary<ulong, MyPlayer> OnlinePlayers = new Dictionary<ulong, MyPlayer>();
            foreach (MyPlayer onlineUser in _onlineUsers)
                OnlinePlayers.Add(onlineUser.Id.SteamId, onlineUser);
            
            
            // Report rewards issued!
            StringBuilder payoutReport = new StringBuilder();
            payoutReport.AppendLine("*   Payout Report   *");
            
            if (!payAll)
            {
                await MainBot.Log.Info("Processing Rewards.");
                for (int userIndex = Instance.Config.RegisteredUsers.Count - 1; userIndex >= 0; userIndex--)
                {
                    RegisteredUsers registeredUser = Instance.Config.RegisteredUsers[userIndex];

                    // see if member already received scheduled payout for today
                    if (registeredUser.LastPayout.DayOfYear == DateTime.Now.DayOfYear) continue;
                    
                    SocketGuildUser member = MainBot.DiscordBot.Guild.GetUser(Instance.Config.RegisteredUsers[userIndex].DiscordId);
                    IReadOnlyCollection<SocketRole> memberRoles = member.Roles;
                    bool registerRewarded = false; // if user received a payout, update their last reward day.

                    int rewardCounter = 0;
                    for (int rewardIndex = Instance.Config.Rewards.Count - 1; rewardIndex >= 0; rewardIndex--)
                    {
                        Reward reward = Instance.Config.Rewards[rewardIndex];

                        // Only pay users with the appropriate role...
                        foreach (SocketRole memberRole in memberRoles)
                        {
                            if (memberRole.Name != reward.CommandRole) continue;
                            
                            // Payday!!
                            string startCommand = reward.Command.Replace("{SteamID}", registeredUser.IngameSteamId.ToString());
                            string finishCommand = startCommand.Replace("{Username}", registeredUser.IngameName);
                            Payout payTheMan = new Payout
                            {
                                ID = IdManager.GetNewPayoutID(),
                                DiscordId = member.Id,
                                DiscordName = member.Username,
                                IngameName = registeredUser.IngameName,
                                SteamID = registeredUser.IngameSteamId,
                                ExpiryDate = DateTime.Now + TimeSpan.FromDays(reward.ExpiresInDays),
                                RewardName = reward.Name,
                                PaymentDate = DateTime.Now,
                                Command = finishCommand
                            };

                            payoutReport.AppendLine($"ID           -> {payTheMan.ID}");
                            payoutReport.AppendLine($"Reward Name  -> {payTheMan.RewardName}");
                            payoutReport.AppendLine($"Discord Name -> {payTheMan.DiscordName}");
                            payoutReport.AppendLine($"Discord ID   -> {payTheMan.DiscordId}");
                            payoutReport.AppendLine($"In-Game Name -> {payTheMan.IngameName}");
                            payoutReport.AppendLine($"SteamID      -> {payTheMan.SteamID}");
                            payoutReport.AppendLine($"Command      -> {payTheMan.Command}");
                            payoutReport.AppendLine($"Expires      -> [{payTheMan.DaysUntilExpired} days] {payTheMan.ExpiryDate.ToShortDateString()}");
                            payoutReport.AppendLine("--------------------------------------------------");

                            rewardCounter++;
                            Instance.Config.Payouts.Add(payTheMan);
                            registerRewarded = true;
                            break;
                        }
                    }

                    if (!registerRewarded) continue;
                    
                    registeredUser.LastPayout = DateTime.Now;
                    if (OnlinePlayers.ContainsKey(registeredUser.IngameSteamId))
                    {
                        // Announce to player in game.
                        ModCommunication.SendMessageTo(new DialogMessage($"Reward Bot", null, null, $"You have {rewardCounter} new reward(s) to claim", "Understood!"), registeredUser.IngameSteamId);
                    }
                    else
                    {
                        if (!Instance.Config.IsBotOnline()) continue;
                        // Announce to player on discord.
                        IUser user = await MainBot.DiscordBot.Client.GetUserAsync(registeredUser.DiscordId);
                        try
                        {
                            await user.SendMessageAsync($"You have {rewardCounter} new reward(s) to claim.");
                        }
                        catch (Exception e)
                        {
                            await MainBot.Log.Warn(e.ToString());
                        }
                    }
                }

                await Instance.Save();
                await MainBot.Log.Info(payoutReport);
                return;
            }
            
            // PAY ALL!!!
            if (rewardID == 0) // ID start at 1 on purpose, no selection sends 0 as default.
            {
                MessageBox.Show("An attempt to force-pay all members a reward has failed, invalid reward selected.","Error",MessageBoxButton.OK,MessageBoxImage.Information);
                return;
            }
            payoutReport.AppendLine("** THIS IS A PAYALL REQUEST **");

            if (Instance.Config.RegisteredUsers.Count == 0)
            {
                MessageBox.Show("No players to receive payout.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            if (Instance.Config.Rewards.Count == 0)
            {
                MessageBox.Show("No rewards to issue.", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            for (int userIndex = Instance.Config.RegisteredUsers.Count - 1; userIndex >= 0; userIndex--)
            {
                if (!Instance.Config.IsBotOnline())
                {
                    await MainBot.Log.Warn("Unable to process rewards while the Discord bot is offline.");
                    return;
                }
                RegisteredUsers registeredUser = Instance.Config.RegisteredUsers[userIndex];
                if (registeredUser == null || registeredUser.DiscordId == 0) continue;
                SocketGuildUser member = MainBot.DiscordBot.Guild.GetUser(registeredUser.DiscordId);
                IReadOnlyCollection<SocketRole> memberRoles = member.Roles;
                int rewardCounter = 0;

                for (int rewardIndex = Instance.Config.Rewards.Count - 1; rewardIndex >= 0; rewardIndex--)
                {
                    Reward reward = Instance.Config.Rewards[rewardIndex];
                    
                    // Only pay users with the appropriate role...
                    foreach (SocketRole memberRole in memberRoles)
                    {
                        if (memberRole.Name != reward.CommandRole) continue;
                        // Payday!!
                        string startCommand = reward.Command.Replace("{SteamID}", registeredUser.IngameSteamId.ToString());
                        string finishCommand = startCommand.Replace("{Username}", registeredUser.IngameName);
                        Payout payTheMan = new Payout
                        {
                            ID = IdManager.GetNewPayoutID(),
                            DiscordId = member.Id,
                            DiscordName = member.Username,
                            IngameName = registeredUser.IngameName,
                            SteamID = registeredUser.IngameSteamId,
                            ExpiryDate = DateTime.Now + TimeSpan.FromDays(reward.ExpiresInDays),
                            RewardName = reward.Name,
                            PaymentDate = DateTime.Now,
                            Command = finishCommand
                        };
                            
                        payoutReport.AppendLine($"ID           -> {payTheMan.ID}");
                        payoutReport.AppendLine($"Reward Name  -> {payTheMan.RewardName}");
                        payoutReport.AppendLine($"Discord Name -> {payTheMan.DiscordName}");
                        payoutReport.AppendLine($"Discord ID   -> {payTheMan.DiscordId}");
                        payoutReport.AppendLine($"In-Game Name -> {payTheMan.IngameName}");
                        payoutReport.AppendLine($"SteamID      -> {payTheMan.SteamID}");
                        payoutReport.AppendLine($"Command      -> {payTheMan.Command}");
                        payoutReport.AppendLine($"Expires      -> [{payTheMan.DaysUntilExpired} days] {payTheMan.ExpiryDate.ToShortDateString()}");
                        payoutReport.AppendLine($"--------------------------------------------------");

                        Instance.Config.Payouts.Add(payTheMan);
                        rewardCounter++;
                    }
                }
                if (rewardCounter == 0) continue;
                
                if (OnlinePlayers.ContainsKey(registeredUser.IngameSteamId))
                {
                    // Announce to player in game.
                    ModCommunication.SendMessageTo(new DialogMessage($"Reward Bot", null, null, $"You have {rewardCounter} new reward(s) to claim", "Understood!"), registeredUser.IngameSteamId);
                }
                else
                {
                    if (!Instance.Config.IsBotOnline()) continue;
                    // Announce to player on discord.
                    IUser user = await MainBot.DiscordBot.Client.GetUserAsync(registeredUser.DiscordId);
                    try
                    {
                        await user.SendMessageAsync($"You have {rewardCounter} new reward(s) to claim.");
                    }
                    catch (Exception e)
                    {
                        await MainBot.Log.Warn(e.ToString());
                    }
                }
                
            }

            await Instance.Save();
            await MainBot.Log.Warn(payoutReport);
        }

        public async Task ManualPayout
            (
                ulong discordID,
                string discordName,
                string inGameName,
                ulong steamID,
                string command,
                int expiresInDays
            )
        {
            StringBuilder payoutReport = new StringBuilder();
            payoutReport.AppendLine("*   Manual Payout Created   *");
            
            string startCommand = command.Replace("{SteamID}", steamID.ToString());
            string finishCommand = startCommand.Replace("{Username}", inGameName);
            Payout payTheMan = new Payout
            {
                ID = IdManager.GetNewPayoutID(),
                DiscordId = discordID,
                DiscordName = discordName,
                IngameName = inGameName,
                SteamID = steamID,
                ExpiryDate = DateTime.Now + TimeSpan.FromDays(expiresInDays),
                RewardName = "Manual Reward",
                PaymentDate = DateTime.Now,
                Command = finishCommand
            };
            
            payoutReport.AppendLine($"ID           -> {payTheMan.ID}");
            payoutReport.AppendLine($"Reward Name  -> {payTheMan.RewardName}");
            payoutReport.AppendLine($"Discord Name -> {payTheMan.DiscordName}");
            payoutReport.AppendLine($"Discord ID   -> {payTheMan.DiscordId}");
            payoutReport.AppendLine($"In-Game Name -> {payTheMan.IngameName}");
            payoutReport.AppendLine($"SteamID      -> {payTheMan.SteamID}");
            payoutReport.AppendLine($"Command      -> {payTheMan.Command}");
            payoutReport.AppendLine($"Expires      -> [{payTheMan.DaysUntilExpired} days] {payTheMan.ExpiryDate.ToShortDateString()}");
            payoutReport.AppendLine($"--------------------------------------------------");

            await MainBot.Log.Warn(payoutReport);
            Instance.Config.Payouts.Add(payTheMan);
            await Instance.Save();
        }
    }
    
    /// <summary>
    /// NLog manager for asynchronous logging.  This works on a set timed queued system to write logs. 
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