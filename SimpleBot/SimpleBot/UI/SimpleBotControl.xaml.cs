using System;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using Discord;
using Discord.WebSocket;
using AsynchronousObservableConcurrentList;
using SimpleBot.Settings;
using static SimpleBot.MainBot;

namespace SimpleBot.UI
{
    public sealed partial class SimpleBotControl : UserControl
    {
        public AsynchronousObservableConcurrentList<SocketGuildUser> FilteredDiscordMembers = new AsynchronousObservableConcurrentList<SocketGuildUser>();
        public static object FilteredDiscordMembers_LOCK = new object();

        public AsynchronousObservableConcurrentList<RegisteredUsers> FilteredRegisteredUsers = new AsynchronousObservableConcurrentList<RegisteredUsers>(); 
        public static object FilteredRegisteredUsers_LOCK = new object();
        
        private delegate void UpdateUI();
        
        public SimpleBotControl()
        {
            BindingOperations.EnableCollectionSynchronization(FilteredDiscordMembers , FilteredDiscordMembers_LOCK);
            BindingOperations.EnableCollectionSynchronization(FilteredRegisteredUsers , FilteredRegisteredUsers_LOCK);
            
            InitializeComponent();
            DataContext = Instance.Config;
            DiscordMembersGrid.DataContext = this;
            RegisteredMembersGrid.DataContext = this;
            FilteredCount.DataContext = this;
            StatusLabel.DataContext = Instance.Config;
            RewardCommandsList.ItemsSource = Instance.Config.Rewards;
            tbRoleComboBox.ItemsSource = MainBot.DiscordBot.Roles;
            Instance.DiscordMembers.CollectionChanged += DiscordMembersOnCollectionChanged;
            Instance.Config.RegisteredUsers.CollectionChanged += RegisteredUsersOnCollectionChanged;
            RegisteredUsersOnCollectionChanged(null, null); // This looks dumb but forces the gridview to load all the data on startup and it works.
        }

        private void RegisteredUsersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DiscordMembersGrid.Dispatcher.BeginInvoke((Action)(() => { RegisteredMembersGrid.ItemsSource = null; }));
            FilteredRegisteredUsers.Clear();
            FilteredRegisteredUsers.AddRange(Instance.Config.RegisteredUsers);
            FilteredRegisteredCount.Dispatcher.BeginInvoke((Action)(() => { FilteredRegisteredCount.Text = FilteredRegisteredUsers.Count.ToString() + " / " + Instance.Config.RegisteredUsers.Count.ToString(); })); 
            DiscordMembersGrid.Dispatcher.BeginInvoke((Action)(() => { RegisteredMembersGrid.ItemsSource = FilteredRegisteredUsers; }));
        }

        private void DiscordMembersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DiscordMembersGrid.Dispatcher.BeginInvoke((Action)(() => {DiscordMembersGrid.ItemsSource = null; })); 
            FilteredDiscordMembers.Clear();
            FilteredDiscordMembers.AddRange(Instance.DiscordMembers);
            FilteredCount.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UpdateUI(UpdateCounts));
            DiscordMembersGrid.Dispatcher.BeginInvoke((Action)(() => {DiscordMembersGrid.ItemsSource = FilteredDiscordMembers; }));
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
            if (Instance.Config.IsBotOnline()) return;
            await MainBot.DiscordBot.Connect();
        }

        private async void ForceBotOffline_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Instance.Config.IsBotOnline()) return;
            await MainBot.DiscordBot.Disconnect();
        }

        private async void ForceBoosterRewardPayout_OnClick(object sender, RoutedEventArgs e)
        {
            if (Instance.Config.EnabledOnline && MainBot.DiscordBot.Guilds.Count >= 1)
                await MainBot.DiscordBot.RewardManager.Payout();
            else
                Log.Warn("Unable to payout rewards.  Bot offline or not in any servers.");
        }

        private void RemoveRegisteredMember_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure, removing this person cannot be undone!", "Remove Registered User", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                return;
            
            Instance.Config.RegisteredUsers.RemoveAt(RegisteredMembersGrid.SelectedIndex);
            Instance.Save();
        }

        private void FilterDiscordMembers_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tempTextBox = (TextBox)sender;
            if (tempTextBox is null)
                return;
            
            DiscordMembersGrid.ItemsSource = null;
            FilteredDiscordMembers.Clear();

            foreach (SocketGuildUser member in Instance.DiscordMembers)
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

                if (!string.IsNullOrEmpty(member.Id.ToString()) && member.Id.ToString().Contains(tempTextBox.Text))
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
            if (tempTextBox is null)
                return;
            
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
            Settings.Reward command = RewardCommandsList.SelectedItem as Settings.Reward;
            ShowCommand.Text = command?.Command;
        }

        private void NewCommand_OnClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(Expires.Text, out int expiredInDays))
            {
                MessageBox.Show("Invalid Entry: Day(s) until expired", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Settings.Reward command = new Settings.Reward
            {
                Name = NewCommandName.Text,
                Command = CommandText.Text,
                CommandRole = tbCommandRole.Text,
                DaysToPay = tbDaysToPay.Text,
                ExpiresInDays = expiredInDays
            };

            Instance.Config.Rewards.Add(command);
            
            StringBuilder logNewCommand = new StringBuilder();
            logNewCommand.AppendLine("New reward command created:");
            logNewCommand.AppendLine($"Name: {command.Name}");
            logNewCommand.AppendLine($"Command: {command.Command}");
            logNewCommand.AppendLine("Saving settings...");
            Instance.Save();
            Log.Info(logNewCommand);
        }

        private async void ForceBoosterRewardPayoutAll_OnClick(object sender, RoutedEventArgs e)
        {
            if ( MessageBox.Show("Are you sure you want to run all the reward commands on ALL players, regardless if they have already received their rewards or not?", "CAUTION!!!", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel) 
                return;
            
            await MainBot.DiscordBot.RewardManager.Payout();
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
            e.Handled = true;
        }

        private void SendDmToPlayer_OnClick(object sender, RoutedEventArgs e)
        {
            
            DmPopup.IsOpen = true;
            SocketGuildUser selectedUser = (SocketGuildUser)DiscordMembersGrid.SelectedItem;
            if (selectedUser == null)
            {
                PopupDialogFinished();
                return;
            }
            
            PopupUserName.Text = string.IsNullOrEmpty(selectedUser.Nickname) ? selectedUser.Username : $"{selectedUser.Username} [{selectedUser.Nickname}]";
        }

        private async void SendMessage_OnClick(object sender, RoutedEventArgs e)
        {
            SocketGuildUser selectedUser = (SocketGuildUser)DiscordMembersGrid.SelectedItem;
            
            if (selectedUser == null)
            {
                PopupDialogFinished();
                MessageBox.Show("No Discord user selected.", "Select Somebody!!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(PopupMessage.Text))
            {
                MessageBox.Show("Enter a message!", "Spamming blank messages.....", MessageBoxButton.OK, MessageBoxImage.Error);
                PopupDialogFinished();
                return;
            }

            if (!Instance.Config.IsBotOnline())
            {
                MessageBox.Show("Bot Offline, Cannot Send Any Messages!", "Error sending direct message", MessageBoxButton.OK, MessageBoxImage.Error);
                PopupDialogFinished();
                return;
            }
            DmPopup.IsOpen = false;
            IUser user = await MainBot.DiscordBot.Client.GetUserAsync(selectedUser.Id);
            string results = await MainBot.DiscordBot.UserUtils.SendDirectMessage(user, PopupMessage.Text);
            MessageBox.Show(results, "Reply from Discord", MessageBoxButton.OK, MessageBoxImage.Information);
            PopupDialogFinished();
        }

        private void PopupDialogFinished()
        {
            PopupUserName.Text = "";
            PopupMessage.Text = "";
            DmPopup.IsOpen = false;
        }

        private void CancelPopup_OnClick(object sender, RoutedEventArgs e)
        {
            PopupDialogFinished();
        }

        private void EnableOnlineCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            switch (cb.IsChecked)
            {
                case null:
                    return;
                default:
                    Instance.Config.EnabledOnline = cb.IsChecked.Value;
                    break;
            }
        }

        private void EnableOfflineCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            switch (cb.IsChecked)
            {
                case null:
                    return;
                default:
                    Instance.Config.EnabledOffline = cb.IsChecked.Value;
                    break;
            }
        }

        private void RemoveBannedUsersFromRegistryCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            switch (cb.IsChecked)
            {
                case null:
                    return;
                default:
                    Instance.Config.RemoveBannedUsersFromRegistry = cb.IsChecked.Value;
                    break;
            }
        }
        
        private async void CreateManualPayout_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ulong.TryParse(tbSteamID.Text, out ulong steamId))
            {
                MessageBox.Show("The SteamID is invalid, try again.  Only numbers are allowed.", "Oopsies!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(tbManualExpires.Text, out int intManualExpires))
            {
                MessageBox.Show("The expiry is invalid, try again.  Only numbers are allowed.", "Oopsies!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await MainBot.DiscordBot.RewardManager.ManualPayout(steamId, tbCommand.Text, intManualExpires);
        }

        private void TbRoleComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbCommandRole.Text = ((ComboBox)sender).SelectionBoxItem.ToString();
        }
    }
}
