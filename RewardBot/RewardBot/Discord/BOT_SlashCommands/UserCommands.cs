using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace RewardBot.DiscordBot.BOT_SlashCommands
{
    public class GuildUserCommandBuilder
    {
        private SocketGuild _guild;

        private GuildUserCommandBuilder() { }  // We're using a factory constructor, leave this blank.

        private async Task<GuildUserCommandBuilder> InitAsync()
        {
            _guild = MainBot.DiscordBot.Guild;
            
            // Link command
            SlashCommandBuilder boostRewardRegister = new SlashCommandBuilder()
                .WithName("rewards-link")
                .WithDescription("Links your SteamID and Discord account. Required for some rewards.");
            
            // Unlink command
            SlashCommandBuilder boostRewardUnregister = new SlashCommandBuilder()
                .WithName("rewards-unlink")
                .WithDescription("Unlink your SteamID and Discord account.");
            
            // List All Available Rewards
            SlashCommandBuilder listRewards = new SlashCommandBuilder()
                .WithName("rewards-list")
                .WithDescription("List your available rewards.");
            
            await BuildCommands(boostRewardRegister);
            await MainBot.Log.Info($"Preparing Command -> {boostRewardRegister.Name}");
            
            await BuildCommands(boostRewardUnregister);
            await MainBot.Log.Info($"Preparing Command -> {boostRewardUnregister.Name}");

            await BuildCommands(listRewards);
            await MainBot.Log.Info($"Preparing Command -> {listRewards.Name}");

            await MainBot.DiscordBot.HelperUtils.SetUserCommands();
            return this;
        }
        
        public static Task<GuildUserCommandBuilder> CreateAsync()
        {
            GuildUserCommandBuilder ret = new GuildUserCommandBuilder();
            return ret.InitAsync();
        }

        private async Task BuildCommands(SlashCommandBuilder command)
        {
            try
            {
                MainBot.DiscordBot.UserCommands.Add(command.Build());
            }
            catch (HttpException exception)
            {
                // Yes, this is a large log but its easier to figure out whats going on
                
                StringBuilder exceptionFormatted = new StringBuilder();
                exceptionFormatted.AppendLine("* Error creating user application command *");
                exceptionFormatted.AppendLine("Command: " + command.Name);
                exceptionFormatted.AppendLine("Reason: " + exception.Reason);
                exceptionFormatted.AppendLine("Message: " + exception.Message);
                exceptionFormatted.AppendLine("HTTP Code: " + exception.HttpCode);
                exceptionFormatted.AppendLine("Discord Code: " + exception.DiscordCode);
                exceptionFormatted.AppendLine("DiscordJsonErrors: ");
                
                foreach (DiscordJsonError error in exception.Errors)
                {
                    foreach (DiscordError discordError in error.Errors)
                    {
                        exceptionFormatted.AppendLine("Message: " + discordError.Message);
                        exceptionFormatted.AppendLine("Code: " + discordError.Code);
                    }
                }

                await MainBot.Log.Error(exceptionFormatted);
            }
        }
    }
}