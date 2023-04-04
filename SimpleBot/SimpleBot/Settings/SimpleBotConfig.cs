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
        private string _botKey = "OTc3ODc0ODk2MjU0MjkxOTY4.GuL4CC.ZsYhI6NuUsDx4e5ju3vWFzNlVse2tQ5MClQ7xY";
        private bool _enabledWhenOnline = true; // Will activate the bot when the server is online (if not already online)
        private bool _enabledWhenOffline; // Will activate the bot immediately when plugin is loaded.
        private BotStatus _botStatus;
        private string _botStatusMessage = "Space Engineers";
        private bool _removeBannedUserFromRegistry;
        private List<LinkRequest> _linkRequests = new List<LinkRequest>();
        private List<Commands> _rewardCommands = new List<Commands>();
        private List<Payout> _payouts = new List<Payout>();
        private int _payoutExpiry = 90;
        
        // Boost Reward Settings
        private ObservableCollection<RegisteredUsers> _registeredUsers = new ObservableCollection<RegisteredUsers>();
        private string _boostRewardsCommand  = "!! NO COMMAND SETUP !!";
        private string _boostRewardsPayDay = "1"; // Day of the month to run the _BoostRewardsCommand
        private DateTime _lastPayout = DateTime.Today;

        public string BotName { get => _botName; set => SetValue(ref _botName, value); }
        public string BotKey { get => _botKey; set => SetValue(ref _botKey, value); }
        public bool EnabledOffline { get => _enabledWhenOffline; set => SetValue(ref _enabledWhenOffline, value); }
        public bool EnabledOnline { get => _enabledWhenOffline; set => SetValue(ref _enabledWhenOnline, value); }
        public BotStatus BotStatus { get => _botStatus; set => SetValue(ref _botStatus, value); }
        public string BotStatusMessage { get => _botStatusMessage; set => SetValue(ref _botStatusMessage, value); }
        public bool RemoveBannedUsersFromRegistry { get => _removeBannedUserFromRegistry; set => SetValue(ref _removeBannedUserFromRegistry, value); }
        public string BoostRewardsCommand { get => _boostRewardsCommand; set => SetValue(ref _boostRewardsCommand, value); }
        public ObservableCollection<RegisteredUsers> RegisteredUsers { get => _registeredUsers; set => SetValue(ref _registeredUsers, value); }
        public string BoostRewardsPayDay { get => _boostRewardsPayDay; set => SetValue(ref _boostRewardsPayDay, value); }
        public DateTime LastPayout { get => _lastPayout; set => SetValue(ref _lastPayout, value); }
        public List<LinkRequest> LinkRequests { get => _linkRequests; set => SetValue(ref _linkRequests, value); }
        public List<Commands> RewardCommands { get => _rewardCommands; set => SetValue(ref _rewardCommands, value); }
        public List<Payout> Payouts { get => _payouts; set => SetValue(ref _payouts, value); }
        public int PayoutExpiry { get => _payoutExpiry; set => SetValue(ref _payoutExpiry, value); }
    }
}
