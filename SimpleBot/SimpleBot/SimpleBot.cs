using NLog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Timers;
using System.Windows.Controls;
using System.Windows.Data;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using SimpleBot.DiscordBot;
using SimpleBot.Settings;
using SimpleBot.UI;
using SimpleBot.Utils;

namespace SimpleBot
{
    public sealed class MainBot : TorchPluginBase, IWpfPlugin
    {
        private const string ConfigFileName = "SimpleBotConfig.cfg";

        private SimpleBotControl _control;
        public UserControl GetControl() => _control ?? (_control = new SimpleBotControl());

        private Persistent<SimpleBotConfig> _config;
        public SimpleBotConfig Config => _config?.Data;
        public static MainBot Instance;
        public static readonly Logger Log = LogManager.GetLogger("Discord Reward Bot");
        public static Bot DiscordBot;
        public ObservableCollection<Members> DiscordMembers = new ObservableCollection<Members>();
        public readonly object DiscordMembersLock = new object();
        public bool WorldOnline;
        public static readonly CommandsManager CommandsManager = new CommandsManager();
        private Timer _scheduledWork = new Timer();
        
        public override async void Init(ITorchBase torch)
        {
            base.Init(torch);
            BindingOperations.EnableCollectionSynchronization(DiscordMembers, DiscordMembersLock);

            SetupConfig();

            TorchSessionManager sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            Save();
            Instance = this;
            Config.SetBotStatus(BotStatusEnum.Offline);
            _scheduledWork.Interval = TimeSpan.FromMinutes(5).TotalMilliseconds; // Easier than doing math to change :)
            _scheduledWork.Elapsed += (sender, args) => { WorkScheduled(); }; 
            _scheduledWork.Start();
            DiscordBot = new Bot();
            if (Config.EnabledOffline)
                await DiscordBot.Connect();
        }

        private async void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    WorldOnline = true;
                    if (Instance.Config.EnabledOnline && !Instance.Config.IsBotOnline())
                        await DiscordBot.Connect();
                    break;

                case TorchSessionState.Unloading:
                    WorldOnline = false;
                    Log.Info("Session Unloading!");
                    await DiscordBot.Disconnect();
                    break;
            }
        }

        private void SetupConfig()
        {
            string configFile = Path.Combine(StoragePath, ConfigFileName);

            try
            {
                _config = Persistent<SimpleBotConfig>.Load(configFile);
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
            {
                Log.Info("Create Default Config, because none was found!");
                _config = new Persistent<SimpleBotConfig>(configFile, new SimpleBotConfig());
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
            string[] paySchedule = Helper.RemoveWhiteSpace(Config.BoostRewardsPayDay).Split(',');
            foreach (string payDate in paySchedule)
            {
                if (!int.TryParse(payDate, out int payDATE)) continue;
                if (payDATE == DateTime.Now.Day)
                    await Helper.Payout();
            }
            
            // Clear expired payouts
            for (int index = Config.Payouts.Count - 1; index >= 0; index--)
            {
                if (Config.Payouts[index].ExpiryDate < DateTime.Now)
                {
                    Log.Info($"Expired payout removed for player {Config.Payouts[index].IngameName}");
                    Config.Payouts.RemoveAt(index);
                }
            }
        }

        public void Save()
        {
            try
            {
                _config.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
        }
    }
}
