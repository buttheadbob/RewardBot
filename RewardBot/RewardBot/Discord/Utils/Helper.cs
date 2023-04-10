using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using static RewardBot.MainBot;

namespace RewardBot.DiscordBot.Utils
{
    public sealed class Helper
    {
        public async Task SetUserCommands()
        {
            IReadOnlyCollection<SocketApplicationCommand> foundCommands = await MainBot.DiscordBot.Guilds[0].GetApplicationCommandsAsync();
            foreach (SocketApplicationCommand command in foundCommands)
            {
                MainBot.DiscordBot.ExistingCommands.Add(command);
            }

            for (int index = MainBot.DiscordBot.UserCommands.Count - 1; index >= 0; index--)
            {
                bool commandExist = false;
                for (int i = MainBot.DiscordBot.ExistingCommands.Count - 1; i >= 0; i--)
                {
                    if (MainBot.DiscordBot.UserCommands[index].Name.ToString() == MainBot.DiscordBot.ExistingCommands[i].Name)
                        commandExist = true;
                }

                if (!commandExist)
                {
                    await MainBot.DiscordBot.Guild.CreateApplicationCommandAsync(MainBot.DiscordBot.UserCommands[index]);
                    await Log.Warn($"Created Command On Discord Server -> {MainBot.DiscordBot.UserCommands[index].Name.ToString()}");
                }
                else
                {
                    // Don't really need this logged
                    //await Log.Debug($"Command Already On Discord Server -> {MainBot.DiscordBot.UserCommands[index].Name.ToString()}");
                }
            }
        }
    }
}