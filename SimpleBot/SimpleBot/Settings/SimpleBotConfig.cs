using AsynchronousObservableConcurrentList;
using Torch;

namespace SimpleBot.Settings
{
    public sealed class SimpleBotConfig : ViewModel
    {
        // Bot Settings
        private string _botName = "Rewards Bot";
        private string _botKey = "";
        private bool _enabledWhenOnline; // Will activate the bot when the server is online (if not already online)
        private bool _enabledWhenOffline; // Will activate the bot immediately when plugin is loaded.
        private string _botStatusMessage = "Space Engineers";
        private bool _removeBannedUserFromRegistry;
        private AsynchronousObservableConcurrentList<LinkRequest> _linkRequests = new AsynchronousObservableConcurrentList<LinkRequest>();
        private BotStatusEnum _botStatus = BotStatusEnum.Offline;
           
        
        
        // Boost Reward Settings
        private AsynchronousObservableConcurrentList<RegisteredUsers> _registeredUsers = new AsynchronousObservableConcurrentList<RegisteredUsers>();
        private string _boostRewardsPayDay = "1"; // Day of the month to run the _BoostRewardsCommand
        private AsynchronousObservableConcurrentList<Reward> _rewardCommands = new AsynchronousObservableConcurrentList<Reward>();
        private AsynchronousObservableConcurrentList<Payout> _payouts = new AsynchronousObservableConcurrentList<Payout>();

        public string BotName { get => _botName; set => SetValue(ref _botName, value); }
        public string BotKey { get => _botKey; set => SetValue(ref _botKey, value); }
        public bool EnabledOffline { get => _enabledWhenOffline; set => SetValue(ref _enabledWhenOffline, value); }
        public bool EnabledOnline { get => _enabledWhenOnline; set => SetValue(ref _enabledWhenOnline, value); }
        public string BotStatusMessage { get => _botStatusMessage; set => SetValue(ref _botStatusMessage, value); }
        public bool RemoveBannedUsersFromRegistry { get => _removeBannedUserFromRegistry; set => SetValue(ref _removeBannedUserFromRegistry, value); }
        public AsynchronousObservableConcurrentList<RegisteredUsers> RegisteredUsers { get => _registeredUsers; set => SetValue(ref _registeredUsers, value); }
        public string BoostRewardsPayDay { get => _boostRewardsPayDay; set => SetValue(ref _boostRewardsPayDay, value); }
        public AsynchronousObservableConcurrentList<LinkRequest> LinkRequests { get => _linkRequests; set => SetValue(ref _linkRequests, value); }
        public AsynchronousObservableConcurrentList<Reward> Rewards { get => _rewardCommands; set => SetValue(ref _rewardCommands, value); }
        public AsynchronousObservableConcurrentList<Payout> Payouts { get => _payouts; set => SetValue(ref _payouts, value); }
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
