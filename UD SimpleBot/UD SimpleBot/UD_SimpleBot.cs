using NLog;
using System;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using UD_SimpleBot.Settings;
using UD_SimpleBot.UI;

namespace UD_SimpleBot
{
    public class SimpleBot : TorchPluginBase, IWpfPlugin
    {
        private static readonly string CONFIG_FILE_NAME = "UD_SimpleBotConfig.cfg";

        private UD_SimpleBotControl _control;
        public UserControl GetControl() => _control ?? (_control = new UD_SimpleBotControl(this));

        private Persistent<UD_SimpleBotConfig> _config;
        public UD_SimpleBotConfig Config => _config?.Data;

        public static SimpleBot Instance;

        public static readonly Logger Log = LogManager.GetLogger(Instance.Config.Bot_Name);

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            SetupConfig();

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            Save();
            Instance = this;
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {

            switch (state)
            {

                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");
                    break;
            }
        }

        private void SetupConfig()
        {

            var configFile = Path.Combine(StoragePath, CONFIG_FILE_NAME);

            try
            {

                _config = Persistent<UD_SimpleBotConfig>.Load(configFile);

            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
            {

                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<UD_SimpleBotConfig>(configFile, new UD_SimpleBotConfig());
                _config.Save();
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
