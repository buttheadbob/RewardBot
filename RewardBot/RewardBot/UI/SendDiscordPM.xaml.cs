using System.Text;
using System.Windows;
using Discord;
using Discord.WebSocket;
using RewardBot.Utils;

namespace RewardBot.RewardBot.UI
{
    public partial class SendDiscordPM : Window
    {
        private SocketGuildUser userToPM;
        public SendDiscordPM(SocketGuildUser user)
        {
            InitializeComponent();
            userToPM = user;
        }

        private async void Send_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Message.Text))
            {
                MessageBox.Show("Enter a message!", "Spamming blank messages.....", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IUser user = await MainBot.DiscordBot.Client.GetUserAsync(userToPM.Id);
            string results = await MainBot.DiscordBot.UserUtils.SendDirectMessage(user, Message.Text);
            Close();
            MessageBox.Show(results, "Reply from Discord", MessageBoxButton.OK, MessageBoxImage.Information);

            StringBuilder logMessage = new StringBuilder();
            logMessage.AppendLine($"DIRECT MESSAGE sent to {user.Username}");
            logMessage.AppendLine("———————————————————————————————————————");
            logMessage.AppendLine(Message.Text);
            logMessage.AppendLine("———————————————————————————————————————");
            
            await MainBot.Log.Info(logMessage);
        }

        private void Cancel_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}