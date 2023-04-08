using NLog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Controls;
using Discord.WebSocket;
using AsynchronousObservableConcurrentList;
using Discord;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using RewardBot.DiscordBot;
using RewardBot.Settings;
using RewardBot.UI;
using RewardBot.Utils;

namespace RewardBot
{
    public sealed class MainBot : TorchPluginBase, IWpfPlugin
    {
        private const string ConfigFileName = "RewardsBotConfig.cfg";
        public RewardBotControl Control;
        public UserControl GetControl() => Control ?? (Control = new RewardBotControl());
        private Persistent<RewardBotConfig> _config;
        public RewardBotConfig Config => _config?.Data;
        public static MainBot Instance;
        private static readonly Logger _log = LogManager.GetLogger("Discord Rewards Bot");
        public static Log Log = new Log(ref _log);
        public static Bot DiscordBot;
        public AsynchronousObservableConcurrentList<SocketGuildUser> DiscordMembers = new AsynchronousObservableConcurrentList<SocketGuildUser>();
        public static ID_Manager IdManager = new ID_Manager();
        
        public bool WorldOnline;
        public static readonly CommandsManager CommandsManager = new CommandsManager();
        private Timer _scheduledWork = new Timer();
        
        
        public override async void Init(ITorchBase torch)
        {
            base.Init(torch);
            SetupConfig();

            TorchSessionManager sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                await Log.Warn("No session manager loaded!");

            await Save();
            Instance = this;
            Config.SetBotStatus(BotStatusEnum.Offline);
            _scheduledWork.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds; // Easier than doing math to change :)
            _scheduledWork.Elapsed += (sender, args) => { WorkScheduled(); }; 
            _scheduledWork.Start();
            DiscordBot = new Bot();
            if (Config.EnabledOffline)
            {
                await DiscordBot.Connect();
                await DiscordBot.Client.SetStatusAsync(UserStatus.AFK);
            }
        }

        private async void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                    await Log.Info("Session Loaded!");
                    WorldOnline = true;
                    if (Config.EnabledOnline && !Instance.Config.IsBotOnline())
                        await DiscordBot.Connect();
                    break;

                case TorchSessionState.Unloading:
                    WorldOnline = false;
                    await Log.Info("Session Unloading!");
                    if (!Config.EnabledOffline)
                        await DiscordBot.Disconnect();
                    else
                        await DiscordBot.Client.SetStatusAsync(UserStatus.AFK);
                    break;
            }
        }

        private async void SetupConfig()
        {
            string configFile = Path.Combine(StoragePath, ConfigFileName);

            try
            {
                _config = Persistent<RewardBotConfig>.Load(configFile);
            }
            catch (Exception e)
            {
                await Log.Warn(e.ToString());
            }

            if (_config?.Data == null)
            {
                await Log.Info("Create Default Config, because none was found!");
                _config = new Persistent<RewardBotConfig>(configFile, new RewardBotConfig());
                _config.Save();
            }
        }

        private async void WorkScheduled()
        {
            // Clear Link Expired Request
            for (int index = Config.LinkRequests.Count - 1; index >= 0; index--)
            {
                LinkRequest linkRequest = Config.LinkRequests[index];
                if (DateTime.Now - linkRequest.Created > TimeSpan.FromHours(24))
                    Config.LinkRequests.RemoveAt(index);
            }
            
            // Check for scheduled payouts
            
            // Clear expired payouts
            for (int index = Config.Payouts.Count - 1; index >= 0; index--)
            {
                if (Config.Payouts[index].ExpiryDate < DateTime.Now)
                {
                    await Log.Info($"Expired payout removed for player {Config.Payouts[index].IngameName}");
                    Config.Payouts.RemoveAt(index);
                }
            }
        }

        public async Task Save()
        {
            try
            {
                _config.Save();
                await Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                await Log.Warn(e.ToString() + "Configuration failed to save");
            }
        }
    }
}
