using System;
using System.Collections.Generic;
using Torch;
using VRage.Collections;

namespace SimpleBot.Settings
{
    public sealed class SimpleBotConfig : ViewModel
    {
        // Bot Settings
        private string _botName = "Rewards Bot";
        private string _botKey = "";
        private bool _enabledWhenOnline = true; // Will activate the bot when the server is online (if not already online)
        private bool _enabledWhenOffline; // Will activate the bot immediately when plugin is loaded.
        private string _botStatusMessage = "Space Engineers";
        private bool _removeBannedUserFromRegistry;
        private List<LinkRequest> _linkRequests = new List<LinkRequest>();
        private BotStatusEnum _botStatus = BotStatusEnum.Offline;
           
        
        
        // Boost Reward Settings
        private ObservableCollection<RegisteredUsers> _registeredUsers = new ObservableCollection<RegisteredUsers>();
        private string _boostRewardsPayDay = "1"; // Day of the month to run the _BoostRewardsCommand
        private DateTime _lastPayout = DateTime.Today;
        private List<Commands> _rewardCommands = new List<Commands>();
        private List<Payout> _payouts = new List<Payout>();
        private int _payoutExpiry = 90;

        public string BotName { get => _botName; set => SetValue(ref _botName, value); }
        public string BotKey { get => _botKey; set => SetValue(ref _botKey, value); }
        public bool EnabledOffline { get => _enabledWhenOffline; set => SetValue(ref _enabledWhenOffline, value); }
        public bool EnabledOnline { get => _enabledWhenOffline; set => SetValue(ref _enabledWhenOnline, value); }
        public string BotStatusMessage { get => _botStatusMessage; set => SetValue(ref _botStatusMessage, value); }
        public bool RemoveBannedUsersFromRegistry { get => _removeBannedUserFromRegistry; set => SetValue(ref _removeBannedUserFromRegistry, value); }
        public ObservableCollection<RegisteredUsers> RegisteredUsers { get => _registeredUsers; set => SetValue(ref _registeredUsers, value); }
        public string BoostRewardsPayDay { get => _boostRewardsPayDay; set => SetValue(ref _boostRewardsPayDay, value); }
        public DateTime LastPayout { get => _lastPayout; set => SetValue(ref _lastPayout, value); }
        public List<LinkRequest> LinkRequests { get => _linkRequests; set => SetValue(ref _linkRequests, value); }
        public List<Commands> RewardCommands { get => _rewardCommands; set => SetValue(ref _rewardCommands, value); }
        public List<Payout> Payouts { get => _payouts; set => SetValue(ref _payouts, value); }
        public int PayoutExpiry { get => _payoutExpiry; set => SetValue(ref _payoutExpiry, value); }
        public BotStatusEnum BotStatus { get => _botStatus; set => SetValue(ref _botStatus, value); } 
        
        public void SetBotStatus(BotStatusEnum newStatus)
        {
            BotStatus = newStatus;
        }
        public bool IsBotOnline()
        {
            return BotStatus == BotStatusEnum.Online;
        }
    }
}
