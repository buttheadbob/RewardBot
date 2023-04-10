using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using RewardBot.Settings;
using Sandbox.Common.ObjectBuilders.Definitions;
using static RewardBot.MainBot;

namespace RewardBot.DiscordBot.BOT_SlashCommands
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
                
                case "rewards-list":
                    await RewardsList(command);
                    break;
                
                default:
                    await Log.Warn($"Received unknown command -> {command.CommandName} from {command.User}.");
                    break;
            }
        }
        
        private static async Task RewardsLink(SocketSlashCommand command)
        {
            bool repeatCode = false;
            bool endLoop = false;
            string code = string.Empty;
            
            while (!endLoop)
            {
                Random generator = new Random(Guid.NewGuid().GetHashCode()); // Yeah.. this amuses me too :)
                code = generator.Next(100000,999999).ToString("D6");
            
                // Doubtful but check if code already used.
                for (int index = Instance.Config.LinkRequests.Count - 1; index >= 0; index--)
                {
                    LinkRequest linkRequest = Instance.Config.LinkRequests[index];
                    if (linkRequest.Code == code)
                        repeatCode = true;
                }

                if (!repeatCode)
                    endLoop = true;
            }
            

            for (int index = Instance.Config.LinkRequests.Count - 1; index >= 0; index--)
            {
                LinkRequest request = Instance.Config.LinkRequests[index];
                if (request.DiscordId != command.User.Id) continue;
                await command.RespondAsync($"You have already received a code. This is valid for up to 24 hours. Please go into the game and type in chat -> !RewardBot Link {request.Code}", ephemeral:true);
                return;
            }

            Instance.Config.LinkRequests.Add(new LinkRequest()
            {
                Created = DateTime.Now,
                Code = code,
                DiscordId = command.User.Id,
                DiscordUsername = command.User.Username
            });
            
            await Instance.Save();

            await command.RespondAsync($"Your link code is {code}. Go into the game and in type the following in chat -> !RewardBot Link {code}", ephemeral:true);
        }

        private static async Task RewardsUnlink(SocketSlashCommand command)
        {
            for (int index = Instance.Config.RegisteredUsers.Count - 1; index >= 0; index--)
            {
                if (Instance.Config.RegisteredUsers[index].DiscordId == command.User.Id)
                {
                    Instance.Config.RegisteredUsers.RemoveAt(index);
                    await Instance.Save();
                    await command.RespondAsync("Your information has been removed.");
                    return;
                }
            }

            await command.RespondAsync("Unable to locate any information linked to your Discord account.", ephemeral: true);
        }

        private static async Task RewardsList(SocketSlashCommand command)
        {
            StringBuilder rewards = new StringBuilder();
            rewards.AppendLine("   *** AVAILABLE REWARDS ***");
            RegisteredUsers user = null;
            for (int index = Instance.Config.RegisteredUsers.Count - 1; index >= 0; index--)
            {
                if (Instance.Config.RegisteredUsers[index].DiscordId != command.User.Id) continue;
                user = Instance.Config.RegisteredUsers[index];
                break;
            }

            if (user == null)
            {
                await command.RespondAsync("Unable to locate you in the linked player registry.  Have you registered?", ephemeral: true);
                return;
            }

            int count = 0;
            for (int index = Instance.Config.Payouts.Count - 1; index >= 0; index--)
            {
                if (Instance.Config.Payouts[index].DiscordId != command.User.Id) continue;
                Payout reward = Instance.Config.Payouts[index];
                rewards.AppendLine($"**ID:** {reward.ID}    **Name:** {reward.RewardName}    *Expires in {reward.DaysUntilExpired} days*");
                count++;
            }
            
            if (count == 0)
                await command.RespondAsync("No rewards available.", ephemeral: true);
            else
            {
                rewards.AppendLine($"{count} rewards available.");
                await command.RespondAsync(rewards.ToString(), ephemeral: true);
            }
        }
    }
    
    
    
}