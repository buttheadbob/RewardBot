using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using SimpleBot.Settings;
using static SimpleBot.MainBot;

namespace SimpleBot.DiscordBot.BOT_SlashCommands
{
    public static class CommandRan
    {
        public static async Task ClientOnSlashCommandExecuted(SocketSlashCommand command)
        {
            switch (command.CommandName)
            {
                case "rewards-link":
                    await RewardsLink(command);
                    break;
                
                case "rewards-unlink":
                    await RewardsUnlink(command);
                    break;
                
                case "rewards-available":
                    await RewardsAvailable(command);
                    break;
                
                default:
                    Log.Warn($"Received unknown command -> {command.CommandName} from {command.User}.");
                    break;
            }
        }
        
        private static async Task RewardsLink(SocketSlashCommand command)
        {
            Random generator = new Random(Guid.NewGuid().GetHashCode()); // Yeah.. this amuses me too :)
            string code = generator.Next(100000,999999).ToString("D6");

            for (int index = Instance.Config.LinkRequests.Count - 1; index >= 0; index--)
            {
                LinkRequest request = Instance.Config.LinkRequests[index];
                if (request.DiscordId != command.User.Id) continue;
                await command.RespondAsync($"You have already received a code. This is valid for up to 24 hours. Please go into the game and type in chat -> !RewardsBot Link {request.Code}", ephemeral:true);
                return;
            }

            Instance.Config.LinkRequests.Add(new LinkRequest()
            {
                Created = DateTime.Now,
                Code = code,
                DiscordId = command.User.Id,
                DiscordUsername = command.User.Username
            });
            
            Instance.Save();

            await command.RespondAsync($"Your link code is {code}. Go into the game and in type the following in chat -> !RewardsBot Link {code}", ephemeral:true);
        }

        private static async Task RewardsUnlink(SocketSlashCommand command)
        {
            for (int index = Instance.Config.RegisteredUsers.Count - 1; index >= 0; index--)
            {
                if (Instance.Config.RegisteredUsers[index].DiscordId == command.User.Id)
                {
                    Instance.Config.RegisteredUsers.RemoveAt(index);
                    Instance.Save();
                    await command.RespondAsync("Your information has been removed.");
                    return;
                }
            }

            await command.RespondAsync("Unable to locate any information linked to your Discord account.");
        }

        private static async Task RewardsAvailable(SocketSlashCommand command)
        {
            int payouts = 0;
            for (int index = Instance.Config.Payouts.Count - 1; index >= 0; index--)
            {
                Payout payout = Instance.Config.Payouts[index];
                if (payout.DiscordId == command.User.Id)
                    payouts++;
            }

            await command.RespondAsync($"You have {payouts} rewards waiting to be claimed in-game.");
        }
    }
    
    
    
}