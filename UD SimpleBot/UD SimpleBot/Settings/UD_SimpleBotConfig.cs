using System;
using System.Collections.Generic;
using Torch;

namespace UD_SimpleBot.Settings
{
    public class UD_SimpleBotConfig : ViewModel
    {
        private string _BOT_NAME;
        private string _BOT_KEY;
        private string _Enabled_WHEN_ONLINE; // Will activate the bot when the server is online (if not already online)
        private string _Enabled_WHEN_OFFLINE; // Will activate the bot immediately when plugin is loaded.
        private Bot_Status _Bot_Status;
        private string _BOT_STATUS_MESSAGE;


        public string Bot_Name { get => _BOT_KEY; set => SetValue(ref _BOT_NAME, value); }
        public string Bot_Key { get => _BOT_KEY; set => SetValue(ref _BOT_KEY, value); }
        public string EnabledOffline { get => _Enabled_WHEN_OFFLINE; set => SetValue(ref _Enabled_WHEN_OFFLINE, value); }
        public string EnabledOnline { get => _Enabled_WHEN_OFFLINE; set => SetValue(ref _Enabled_WHEN_ONLINE, value); }
        public Bot_Status BotStatus { get => _Bot_Status; set => SetValue(ref _Bot_Status, value); }
        public string Bot_StatusMessage { get => Bot_StatusMessage; set => SetValue(ref _BOT_STATUS_MESSAGE, value); }
    }
}
