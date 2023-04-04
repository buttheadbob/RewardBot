using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using SimpleBot.Settings;
using SimpleBot.Utils;
using static SimpleBot.MainBot;

namespace SimpleBot.UI
{
    public sealed partial class SimpleBotControl : UserControl
    {
        public ObservableCollection<Members> FilteredDiscordMembers = new ObservableCollection<Members>();
        public readonly object FilteredDiscordMembersLock = new object();
        public ObservableCollection<RegisteredUsers> FilteredRegisteredUsers = new ObservableCollection<RegisteredUsers>();
        public readonly object FilteredRegisteredUsersLock = new object();

        private delegate void UpdateUI();
        
        public SimpleBotControl()
        {
            InitializeComponent();
            BindingOperations.EnableCollectionSynchronization(FilteredDiscordMembers, FilteredDiscordMembersLock);
            BindingOperations.EnableCollectionSynchronization(FilteredRegisteredUsers, FilteredRegisteredUsersLock);
            DataContext = Instance.Config;
            DiscordMembersGrid.DataContext = this;
            DiscordMembersGrid.ItemsSource = FilteredDiscordMembers;
            RegisteredMembersGrid.DataContext = this;
            RegisteredMembersGrid.ItemsSource = FilteredRegisteredUsers;
            FilteredCount.DataContext = this;
            RewardCommandsList.ItemsSource = Instance.Config.RewardCommands;
            Instance.DiscordMembers.CollectionChanged += DiscordMembersOnCollectionChanged;
            Instance.Config.RegisteredUsers.CollectionChanged += RegisteredUsersOnCollectionChanged;
        }

        private void RegisteredUsersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            FilteredRegisteredUsers.Clear();
            foreach (RegisteredUsers registeredUser in Instance.Config.RegisteredUsers)
            {
                FilteredRegisteredUsers.Add(registeredUser);
            }

            FilteredRegisteredCount.Text = FilteredRegisteredUsers.Count.ToString() + " / " + Instance.Config.RegisteredUsers.Count.ToString();
        }

        private void DiscordMembersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            lock (FilteredDiscordMembersLock)
            {
                FilteredDiscordMembers.Clear();
                foreach (Members member in Instance.DiscordMembers)
                {
                    FilteredDiscordMembers.Add(member);
                }
            }

            FilteredCount.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UpdateUI(UpdateCounts));
        }

        private void UpdateCounts()
        {
            FilteredCount.Text = FilteredDiscordMembers.Count.ToString() + " / " + Instance.DiscordMembers.Count.ToString();
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            Instance.Save();
        }

        private async void ForceBotOnline_OnClick(object sender, RoutedEventArgs e)
        {
            if (Instance.Config.BotStatus != BotStatus.Offline) return;
            await MainBot.DiscordBot.Connect();
            Instance.Config.BotStatus = BotStatus.Online;
        }

        private async void ForceBotOffline_OnClick(object sender, RoutedEventArgs e)
        {
            if (Instance.Config.BotStatus == BotStatus.Online)
                await MainBot.DiscordBot.Disconnect();
        }

        private async void ForceBoosterRewardPayout_OnClick(object sender, RoutedEventArgs e)
        {
            if (Instance.Config.EnabledOnline && MainBot.DiscordBot.Guilds.Count >= 1)
                await MainBot.DiscordBot.BoostReward.Payout();
            else
                Log.Warn("Unable to payout rewards.  Bot offline or not in any servers.");
        }

        private void RemoveRegisteredMember_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure, removing this person cannot be undone!", "Remove Registered User", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                return;
            
            Instance.Config.RegisteredUsers.RemoveAt(RegisteredMembersGrid.SelectedIndex);
        }

        private void FilterDiscordMembers_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tempTextBox = (TextBox)sender;
            DiscordMembersGrid.ItemsSource = null;
            FilteredDiscordMembers.Clear();

            foreach (Members member in Instance.DiscordMembers)
            {
                if (!string.IsNullOrEmpty(member.Nickname) && member.Nickname.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    FilteredDiscordMembers.Add(member);
                    continue;
                }

                if (!string.IsNullOrEmpty(member.Username) && member.Username.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    FilteredDiscordMembers.Add(member);
                    continue;
                }

                if (!string.IsNullOrEmpty(member.UserId.ToString()) && member.UserId.ToString().Contains(tempTextBox.Text))
                {
                    FilteredDiscordMembers.Add(member);
                    continue;
                }
            }

            FilteredCount.Text = FilteredDiscordMembers.Count.ToString() + " / " + Instance.DiscordMembers.Count.ToString();
            DiscordMembersGrid.ItemsSource = FilteredDiscordMembers;
        }

        private void FilterRegisteredMembers_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tempTextBox = (TextBox)sender;
            RegisteredMembersGrid.ItemsSource = null;
            FilteredRegisteredUsers.Clear();

            foreach (RegisteredUsers registeredMember in Instance.Config.RegisteredUsers)
            {
                if (!string.IsNullOrEmpty(registeredMember.DiscordUsername) && registeredMember.DiscordUsername.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    FilteredRegisteredUsers.Add(registeredMember);
                    continue;
                }

                if (!string.IsNullOrEmpty(registeredMember.DiscordId.ToString()) && registeredMember.DiscordId.ToString().Contains(tempTextBox.Text.ToLower()))
                {
                    FilteredRegisteredUsers.Add(registeredMember);
                    continue;
                }

                if (!string.IsNullOrEmpty(registeredMember.IngameSteamId.ToString()) && registeredMember.IngameSteamId.ToString().Contains(tempTextBox.Text))
                {
                    FilteredRegisteredUsers.Add(registeredMember);
                    continue;
                }

                if (!string.IsNullOrEmpty(registeredMember.IngameName) &&
                    registeredMember.IngameName.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    FilteredRegisteredUsers.Add(registeredMember);
                    continue;
                }
            }

            FilteredCount.Text = FilteredRegisteredUsers.Count.ToString() + " / " + Instance.Config.RegisteredUsers.Count.ToString();
            RegisteredMembersGrid.ItemsSource = FilteredRegisteredUsers;
        }

        private void RewardCommandsList_OnSelected(object sender, RoutedEventArgs e)
        {
            Settings.Commands command = RewardCommandsList.SelectedItem as Settings.Commands;
            ShowCommand.Text = command?.Command;
        }

        private void NewCommand_OnClick(object sender, RoutedEventArgs e)
        {
            Settings.Commands command = new Settings.Commands();
            command.Name = NewCommandName.Text;
            command.Command = CommandText.Text;
            
            Instance.Config.RewardCommands.Add(command);
            
            StringBuilder logNewCommand = new StringBuilder();
            logNewCommand.AppendLine("New reward command created:");
            logNewCommand.AppendLine($"Name: {command.Name}");
            logNewCommand.AppendLine($"Command: {command.Command}");
            logNewCommand.AppendLine("Saving settings...");
            Instance.Save();
            MainBot.Log.Info(logNewCommand);
        }

        private async void ForceBoosterRewardPayoutAll_OnClick(object sender, RoutedEventArgs e)
        {
            if ( MessageBox.Show("Are you sure you want to run all the reward commands on ALL players, regardless if they have already received their rewards or not?", "CAUTION!!!", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel) 
                return;
            
            await Helper.Payout(true);
        }
    }
}
