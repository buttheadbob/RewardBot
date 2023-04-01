using System;
using System.Collections.Generic;
using Torch;

namespace UD_SimpleBot.Settings
{
    public class UD_SimpleBotConfig : ViewModel
    {
        // Bot Settings
        private string _BOT_NAME;
        private string _BOT_KEY;
        private string _Enabled_WHEN_ONLINE; // Will activate the bot when the server is online (if not already online)
        private string _Enabled_WHEN_OFFLINE; // Will activate the bot immediately when plugin is loaded.
        private Bot_Status _Bot_Status;
        private string _BOT_STATUS_MESSAGE;
        private bool _Only_Reward_Registered_Boosters;
        private bool _Remove_BannedUser_FromRegistry;

        // Reward Settings
        private List<RegisteredUsers> _RegisteredUsers = new List<RegisteredUsers>();


        public string Bot_Name { get => _BOT_KEY; set => SetValue(ref _BOT_NAME, value); }
        public string Bot_Key { get => _BOT_KEY; set => SetValue(ref _BOT_KEY, value); }
        public string EnabledOffline { get => _Enabled_WHEN_OFFLINE; set => SetValue(ref _Enabled_WHEN_OFFLINE, value); }
        public string EnabledOnline { get => _Enabled_WHEN_OFFLINE; set => SetValue(ref _Enabled_WHEN_ONLINE, value); }
        public Bot_Status BotStatus { get => _Bot_Status; set => SetValue(ref _Bot_Status, value); }
        public string Bot_StatusMessage { get => Bot_StatusMessage; set => SetValue(ref _BOT_STATUS_MESSAGE, value); }
        public bool Only_Reward_Registered_Boosters { get => _Only_Reward_Registered_Boosters; set => SetValue(ref _Only_Reward_Registered_Boosters, value); }
        public bool RemoveBannedUsers_FromRegistry { get => _Remove_BannedUser_FromRegistry; set => SetValue(ref _Remove_BannedUser_FromRegistry, value); }


        public List<RegisteredUsers> RegisteredUsers { get => _RegisteredUsers; set => SetValue(ref _RegisteredUsers, value); }
    }
}
