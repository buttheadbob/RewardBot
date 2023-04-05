using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using Discord;
using SimpleBot.Settings;
using SimpleBot.Utils;
using static SimpleBot.MainBot;

namespace SimpleBot.UI
{
    public sealed partial class SimpleBotControl : UserControl
    {
        private ObservableCollection<Members> _filteredDiscordMembers = new ObservableCollection<Members>();
        private readonly object _filteredDiscordMembersLock = new object();
        private ObservableCollection<RegisteredUsers> _filteredRegisteredUsers = new ObservableCollection<RegisteredUsers>();
        private readonly object _filteredRegisteredUsersLock = new object();

        private delegate void UpdateUI();
        
        public SimpleBotControl()
        {
            InitializeComponent();
            BindingOperations.EnableCollectionSynchronization(_filteredDiscordMembers, _filteredDiscordMembersLock);
            BindingOperations.EnableCollectionSynchronization(_filteredRegisteredUsers, _filteredRegisteredUsersLock);
            DataContext = Instance.Config;
            DiscordMembersGrid.DataContext = this;
            DiscordMembersGrid.ItemsSource = _filteredDiscordMembers;
            RegisteredMembersGrid.DataContext = this;
            RegisteredMembersGrid.ItemsSource = _filteredRegisteredUsers;
            FilteredCount.DataContext = this;
            StatusLabel.DataContext = Instance.Config;
            RewardCommandsList.ItemsSource = Instance.Config.RewardCommands;
            Instance.DiscordMembers.CollectionChanged += DiscordMembersOnCollectionChanged;
            Instance.Config.RegisteredUsers.CollectionChanged += RegisteredUsersOnCollectionChanged;
        }

        private void RegisteredUsersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _filteredRegisteredUsers.Clear();
            foreach (RegisteredUsers registeredUser in Instance.Config.RegisteredUsers)
            {
                _filteredRegisteredUsers.Add(registeredUser);
            }

            FilteredRegisteredCount.Text = _filteredRegisteredUsers.Count.ToString() + " / " + Instance.Config.RegisteredUsers.Count.ToString();
        }

        private void DiscordMembersOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            lock (_filteredDiscordMembersLock)
            {
                _filteredDiscordMembers.Clear();
                foreach (Members member in Instance.DiscordMembers)
                {
                    _filteredDiscordMembers.Add(member);
                }
            }

            FilteredCount.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UpdateUI(UpdateCounts));
        }

        private void UpdateCounts()
        {
            FilteredCount.Text = _filteredDiscordMembers.Count.ToString() + " / " + Instance.DiscordMembers.Count.ToString();
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
                await Helper.Payout(true);
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
            DiscordMembersGrid.ItemsSource = null;
            _filteredDiscordMembers.Clear();

            foreach (Members member in Instance.DiscordMembers)
            {
                if (!string.IsNullOrEmpty(member.Nickname) && member.Nickname.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    _filteredDiscordMembers.Add(member);
                    continue;
                }

                if (!string.IsNullOrEmpty(member.Username) && member.Username.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    _filteredDiscordMembers.Add(member);
                    continue;
                }

                if (!string.IsNullOrEmpty(member.UserId.ToString()) && member.UserId.ToString().Contains(tempTextBox.Text))
                {
                    _filteredDiscordMembers.Add(member);
                    continue;
                }
            }

            FilteredCount.Text = _filteredDiscordMembers.Count.ToString() + " / " + Instance.DiscordMembers.Count.ToString();
            DiscordMembersGrid.ItemsSource = _filteredDiscordMembers;
        }

        private void FilterRegisteredMembers_OnKeyUp(object sender, KeyEventArgs e)
        {
            TextBox tempTextBox = (TextBox)sender;
            RegisteredMembersGrid.ItemsSource = null;
            _filteredRegisteredUsers.Clear();

            foreach (RegisteredUsers registeredMember in Instance.Config.RegisteredUsers)
            {
                if (!string.IsNullOrEmpty(registeredMember.DiscordUsername) && registeredMember.DiscordUsername.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    _filteredRegisteredUsers.Add(registeredMember);
                    continue;
                }

                if (!string.IsNullOrEmpty(registeredMember.DiscordId.ToString()) && registeredMember.DiscordId.ToString().Contains(tempTextBox.Text.ToLower()))
                {
                    _filteredRegisteredUsers.Add(registeredMember);
                    continue;
                }

                if (!string.IsNullOrEmpty(registeredMember.IngameSteamId.ToString()) && registeredMember.IngameSteamId.ToString().Contains(tempTextBox.Text))
                {
                    _filteredRegisteredUsers.Add(registeredMember);
                    continue;
                }

                if (!string.IsNullOrEmpty(registeredMember.IngameName) &&
                    registeredMember.IngameName.ToLower().Contains(tempTextBox.Text.ToLower()))
                {
                    _filteredRegisteredUsers.Add(registeredMember);
                    continue;
                }
            }

            FilteredCount.Text = _filteredRegisteredUsers.Count.ToString() + " / " + Instance.Config.RegisteredUsers.Count.ToString();
            RegisteredMembersGrid.ItemsSource = _filteredRegisteredUsers;
        }

        private void RewardCommandsList_OnSelected(object sender, RoutedEventArgs e)
        {
            Settings.Commands command = RewardCommandsList.SelectedItem as Settings.Commands;
            ShowCommand.Text = command?.Command;
        }

        private void NewCommand_OnClick(object sender, RoutedEventArgs e)
        {
            Settings.Commands command = new Settings.Commands
            {
                Name = NewCommandName.Text,
                Command = CommandText.Text
            };

            Instance.Config.RewardCommands.Add(command);
            
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
            
            await Helper.Payout(true);
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
            e.Handled = true;
        }

        private void SendDmToPlayer_OnClick(object sender, RoutedEventArgs e)
        {
            
            DmPopup.IsOpen = true;
            Members selectedUser = (Members)DiscordMembersGrid.SelectedItem;
            if (selectedUser == null)
            {
                PopupDialogFinished();
                return;
            }
            
            PopupUserName.Text = string.IsNullOrEmpty(selectedUser.Nickname) ? selectedUser.Username : $"{selectedUser.Username} [{selectedUser.Nickname}]";
        }

        private async void SendMessage_OnClick(object sender, RoutedEventArgs e)
        {
            Members selectedUser = (Members)DiscordMembersGrid.SelectedItem;
            
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
            IUser user = await MainBot.DiscordBot.Client.GetUserAsync(selectedUser.UserId);
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
    }
}
