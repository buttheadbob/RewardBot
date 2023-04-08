using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace RewardBot.DiscordBot.BOT_SlashCommands
{
    public class GuildCommandBuilder
    {
        private SocketGuild _guild;

        private GuildCommandBuilder() { }  // We're using a factory constructor, leave this blank.

        private async Task<GuildCommandBuilder> InitAsync()
        {
            _guild = MainBot.DiscordBot.Client.GetGuild(MainBot.DiscordBot.Guilds[0].Id);
            
            // Link command
            SlashCommandBuilder boostRewardRegister = new SlashCommandBuilder();
            boostRewardRegister.WithName("rewards-link");
            boostRewardRegister.WithDescription("Links your SteamID and Discord account. Required for some rewards.");
            
            // Unlink command
            SlashCommandBuilder boostRewardUnregister = new SlashCommandBuilder();
            boostRewardUnregister.WithName("rewards-unlink");
            boostRewardUnregister.WithDescription("Unlink your SteamID and Discord account.");
            
            // How many rewards available
            SlashCommandBuilder countRewards = new SlashCommandBuilder();
            countRewards.WithName("rewards-available");
            countRewards.WithDescription("Tells you how many rewards you have available.");
            
            // List All Available Rewards
            SlashCommandBuilder listRewards = new SlashCommandBuilder();
            listRewards.WithName("rewards-list");
            listRewards.WithDescription("List your available rewards.");
            
            await BuildCommands(boostRewardRegister);
            await MainBot.Log.Info($"Preparing Command -> {boostRewardRegister.Name}");
            
            await BuildCommands(boostRewardUnregister);
            await MainBot.Log.Info($"Preparing Command -> {boostRewardUnregister.Name}");

            await BuildCommands(countRewards);
            await MainBot.Log.Info($"Preparing Command -> {countRewards.Name}");

            await BuildCommands(listRewards);
            await MainBot.Log.Info($"Preparing Command -> {listRewards.Name}");

            await MainBot.DiscordBot.HelperUtils.SetGuildCommands();
            return this;
        }
        
        public static Task<GuildCommandBuilder> CreateAsync()
        {
            GuildCommandBuilder ret = new GuildCommandBuilder();
            return ret.InitAsync();
        }

        private async Task BuildCommands(SlashCommandBuilder command)
        {
            try
            {
                MainBot.DiscordBot.AllCommands.Add(command.Build());
            }
            catch (HttpException exception)
            {
                // Yes, this is a large log but its easier to figure out whats going on
                // since UD doesn't have any discord bot developers.
                
                StringBuilder exceptionFormatted = new StringBuilder();
                exceptionFormatted.AppendLine("* Error creating application command *");
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