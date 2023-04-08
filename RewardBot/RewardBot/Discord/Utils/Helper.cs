using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using static RewardBot.MainBot;

namespace RewardBot.DiscordBot.Utils
{
    public sealed class Helper
    {
        public async Task SetGuildCommands()
        {
            IReadOnlyCollection<SocketApplicationCommand> foundCommands = await MainBot.DiscordBot.Guilds[0].GetApplicationCommandsAsync();
            foreach (SocketApplicationCommand command in foundCommands)
            {
                MainBot.DiscordBot.ExistingCommands.Add(command);
            }

            for (int index = MainBot.DiscordBot.AllCommands.Count - 1; index >= 0; index--)
            {
                bool commandExist = false;
                for (int i = MainBot.DiscordBot.ExistingCommands.Count - 1; i >= 0; i--)
                {
                    if (MainBot.DiscordBot.AllCommands[index].Name.ToString() == MainBot.DiscordBot.ExistingCommands[i].Name)
                        commandExist = true;
                }

                if (!commandExist)
                {
                    await MainBot.DiscordBot.Guilds[0].CreateApplicationCommandAsync(MainBot.DiscordBot.AllCommands[index]);
                    await Log.Info($"Created Command On Discord Server -> {MainBot.DiscordBot.AllCommands[index].Name.ToString()}");
                }
                else
                {
                    // Don't really need this logged but we can still log it silently.
                    await Log.Debug($"Command Already On Discord Server -> {MainBot.DiscordBot.AllCommands[index].Name.ToString()}");
                }
            }
        }
    }
}