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
using RewardBot.DiscordBot.BOT_SlashCommands;
using RewardBot.RewardBot.UI;
using RewardBot.Settings;
using static RewardBot.MainBot;

namespace RewardBot.UI
{
    public sealed partial class RewardBotControl : UserControl
    {
        public AsynchronousObservableConcurrentList<SocketGuildUser> FilteredDiscordMembers = new AsynchronousObservableConcurrentList<SocketGuildUser>();
        public static object FilteredDiscordMembers_LOCK = new object();

        public AsynchronousObservableConcurrentList<RegisteredUsers> FilteredRegisteredUsers = new AsynchronousObservableConcurrentList<RegisteredUsers>(); 
        public static object FilteredRegisteredUsers_LOCK = new object();
        
        private int lastSelectedItem = -1; // Used to store last selected payout for the delete payout button.
        
        private delegate void UpdateUI();
        
        public RewardBotControl()
        {
            BindingOperations.EnableCollectionSynchronization(FilteredDiscordMembers , FilteredDiscordMembers_LOCK);
            BindingOperations.EnableCollectionSynchronization(FilteredRegisteredUsers , FilteredRegisteredUsers_LOCK);
            
            InitializeComponent();
            UiDispatcher = Dispatcher;
            DataContext = Instance.Config;
            DiscordMembersGrid.DataContext = this;
            RegisteredMembersGrid.DataContext = this;
            FilteredCount.DataContext = this;
            StatusLabel.DataContext = Instance.Config;
            RewardCommandsList.ItemsSource = Instance.Config.Rewards;
            tbRoleComboBox.DataContext = MainBot.DiscordBot;
            ForceSelectedPayoutToAll.DataContext = Instance.Config;
            ForceSelectedPayoutToAll.ItemsSource = Instance.Config.Rewards;
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

        private async void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            await Instance.Save();
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
            if (Instance.Config.EnabledOnline)
                await RewardManager.Payout();
            else
                await Log.Warn("Unable to payout rewards.  Bot offline or not in any servers.");
        }

        private async void RemoveRegisteredMember_OnClick(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure, removing this person cannot be undone!", "Remove Registered User", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                return;
            
            Instance.Config.RegisteredUsers.RemoveAt(RegisteredMembersGrid.SelectedIndex);
            await Instance.Save();
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
            Reward command = RewardCommandsList.SelectedItem as Reward;
            ShowCommand.Text = command?.Command;
        }

        private async void NewCommand_OnClick(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(Expires.Text, out int expiredInDays))
            {
                MessageBox.Show("Invalid Entry: Day(s) until expired", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Reward reward = new Reward
            {
                ID = MainBot.IdManager.GetNewRewardID(),
                Name = NewCommandName.Text,
                Command = CommandText.Text,
                CommandRole = tbCommandRole.Text,
                DaysToPay = tbDaysToPay.Text,
                ExpiresInDays = expiredInDays
            };

            Instance.Config.Rewards.Add(reward);
            
            StringBuilder logNewCommand = new StringBuilder();
            logNewCommand.AppendLine("New reward command created:");
            logNewCommand.AppendLine($"ID: {reward.ID}");
            logNewCommand.AppendLine($"Name: {reward.Name}");
            logNewCommand.AppendLine($"Command: {reward.Command}");
            logNewCommand.AppendLine("Saving settings...");
            await Instance.Save();
            await Log.Info(logNewCommand);
        }

        private async void ForceBoosterRewardPayoutAll_OnClick(object sender, RoutedEventArgs e)
        {
            if (ForceSelectedPayoutToAll.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a reward command to run on all players.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            
            if ( MessageBox.Show($"Are you sure you want to run the reward command [{Instance.Config.Rewards[ForceSelectedPayoutToAll.SelectedIndex].Name}] on ALL players, regardless if they have already received their rewards or not?  This will not count towards their scheduled reward payments.", "CAUTION!!!", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.Cancel) 
                return;
            
            await RewardManager.Payout(Instance.Config.Rewards[ForceSelectedPayoutToAll.SelectedIndex].ID, true);
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
            e.Handled = true;
        }

        private void SendDmToPlayer_OnClick(object sender, RoutedEventArgs e)
        {
            if (DiscordMembersGrid.SelectedIndex == -1) return;
            
            SendDiscordPM SendDMWindow = new SendDiscordPM((SocketGuildUser)DiscordMembersGrid.SelectedItem);
            SendDMWindow.ShowDialog();
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

            if (!ulong.TryParse(tbDiscordID.Text, out ulong discordId))
            {
                MessageBox.Show("The DiscordID is invalid, try again.  Only numbers are allowed.", "Oopsies!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!int.TryParse(tbManualExpires.Text, out int intManualExpires))
            {
                MessageBox.Show("The expiry is invalid, try again.  Only numbers are allowed.", "Oopsies!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (intManualExpires <= 0)
            {
                MessageBox.Show("The expiry is invalid, try again.  Cannot be equal to or less than 0.", "Oopsies!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (intManualExpires > 365)
            {
                MessageBox.Show("The expiry is invalid, a payout cannot last longer than 1 year (365 days).", "Even the Matrix has its limitations Mr.Anderson!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            await RewardManager.ManualPayout(discordId, tbDiscordName.Text, tbInGameName.Text, steamId, tbCommand.Text, intManualExpires);
        }

        private void TbRoleComboBox_OnSelectionChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            tbCommandRole.Text = ((SocketRole)tbRoleComboBox.SelectedItem).Name;
        }

        private async void EditPayout_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ulong.TryParse(TbEditSteamId.Text, out ulong updatedSteamID))
            {
                MessageBox.Show("Check your SteamID value.  Cannot convert.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (!ulong.TryParse(TbEditDiscordID.Text, out ulong updatedDiscordID))
            {
                MessageBox.Show("Check your Discord ID value.  Cannot convert.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (!int.TryParse(TbEditExpiry.Text, out int newExpiryInDays))
            {
                MessageBox.Show("Check your Days until expired value.  Cannot convert.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            if (newExpiryInDays <= 0)
            {
                MessageBox.Show("The expiry is invalid, try again.  Cannot be equal to or less than 0.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            
            if (newExpiryInDays > 365)
            {
                MessageBox.Show("The expiry is invalid, a payout cannot last longer than 1 year (365 days).", "Even the Matrix has its limitations Mr.Anderson!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Payout originalReward = Instance.Config.Payouts[PayoutList.SelectedIndex];

            StringBuilder logReport = new StringBuilder();
            logReport.AppendLine($"Player Reward has been changed!");
            logReport.AppendLine($" ** Original ** ");
            logReport.AppendLine($"ID           -> {originalReward.ID}");
            logReport.AppendLine($"In-Game Name -> {originalReward.IngameName}");
            logReport.AppendLine($"SteamID      -> {originalReward.SteamID}");
            logReport.AppendLine($"Discord Name -> {originalReward.DiscordName}");
            logReport.AppendLine($"Discord ID   -> {originalReward.DiscordId}");
            logReport.AppendLine($"Expiry       -> [{(originalReward.ExpiryDate - DateTime.Now).Days}]{originalReward.ExpiryDate}");
            
            int indexEdit = PayoutList.SelectedIndex;
            if (!Instance.Config.Payouts[indexEdit].ChangeDaysUntilExpire(newExpiryInDays, out string error))
            {
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            Instance.Config.Payouts[indexEdit].IngameName = TbEditInGameName.Text;
            Instance.Config.Payouts[indexEdit].SteamID = updatedSteamID;
            Instance.Config.Payouts[indexEdit].DiscordName = TbEditDiscordName.Text;
            Instance.Config.Payouts[indexEdit].DiscordId = updatedDiscordID;
            Instance.Config.Payouts[indexEdit].Command = tbEditCommand.Text;
            Instance.Config.Payouts[indexEdit].ExpiryDate = DateTime.Now + TimeSpan.FromDays(newExpiryInDays); 
            

            logReport.AppendLine("");
            logReport.AppendLine(" ** Updated **");
            logReport.AppendLine($"ID           -> {originalReward.ID}");
            logReport.AppendLine($"In-Game Name -> {TbEditInGameName.Text}");
            logReport.AppendLine($"SteamID      -> {TbEditSteamId.Text}");
            logReport.AppendLine($"Discord Name -> {TbEditDiscordName.Text}");
            logReport.AppendLine($"Discord ID   -> {updatedDiscordID}");
            logReport.AppendLine($"Expiry       -> [{newExpiryInDays} days]{DateTime.Now + TimeSpan.FromDays(newExpiryInDays)}");
            
            await Log.Warn(logReport);
            PayoutList.ItemsSource = null;
            PayoutList.ItemsSource = Instance.Config.Payouts;

            await Instance.Save();
        }

        private void PayoutList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Payout editPayout = (Payout)PayoutList.SelectedItem;
            if (editPayout == null) return;
            lastSelectedItem = PayoutList.SelectedIndex;
            
            TbEditInGameName.Text = editPayout.IngameName;
            TbEditSteamId.Text = editPayout.SteamID.ToString();
            TbEditDiscordName.Text = editPayout.DiscordName;
            TbEditDiscordID.Text = editPayout.DiscordId.ToString();
            tbEditCommand.Text = editPayout.Command;
            TbEditExpiry.Text = (editPayout.ExpiryDate - DateTime.Now).Days.ToString();
        }

        private async void DeletePayout_OnClick(object sender, RoutedEventArgs e)
        {
            if (lastSelectedItem == -1) return;
            Payout payout = (Payout) PayoutList.Items.GetItemAt(lastSelectedItem);
            if (payout == null) return;
            StringBuilder logDeletePayout = new StringBuilder();
            logDeletePayout.AppendLine("Player Reward Manually Deleted:");
            logDeletePayout.AppendLine($"In-Game Name -> {payout.IngameName}");
            logDeletePayout.AppendLine($"SteamID      -> {payout.SteamID.ToString()}");
            logDeletePayout.AppendLine($"Discord Name -> {payout.DiscordName}");
            logDeletePayout.AppendLine($"Discord ID   -> {payout.DiscordId.ToString()}");
            logDeletePayout.AppendLine($"Command      -> {payout.Command}");
            logDeletePayout.AppendLine($"Expires      -> ({payout.DaysUntilExpired.ToString()} days)  {Instance.Config.Payouts[PayoutList.SelectedIndex].ExpiryDate}");

            await Log.Warn(logDeletePayout);
            UiDispatcher.InvokeAsync(() => { Instance.Config.Payouts.Remove(payout); }); 
            await Instance.Save();
        }

        private async void DeleteSelectedReward_OnClick(object sender, RoutedEventArgs e)
        {
            Reward reward = (Reward) RewardCommandsList.SelectedItem;
            RewardCommandsList.ItemsSource = null;
            Instance.Config.Rewards.Remove(reward);
            RewardCommandsList.ItemsSource = Instance.Config.Rewards;
            await Instance.Save();
        }
    }
}
