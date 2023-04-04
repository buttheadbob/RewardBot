using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using SimpleBot.Settings;
using static SimpleBot.MainBot;

namespace SimpleBot.DiscordBot.Rewards
{
    public class RewardBoosters
    {
        public Task Payout()
        {
            if (Instance.Config.BotStatus != BotStatus.Online)
            {
                Log.Warn($"{Instance.Config.BotName} is offline, unable to payout BoostRewards.");
                return Task.CompletedTask;
            }

            // Reward only registered users because we need them to link their SteamID to their discord account.
            for (int index = Instance.Config.RegisteredUsers.Count - 1; index >= 0; index--)
            {
                RegisteredUsers user = Instance.Config.RegisteredUsers[index];
                SocketGuildUser guildUser = MainBot.DiscordBot.Guilds[0].GetUser(user.DiscordId);
                
                if (guildUser == null)
                {
                    Log.Info($"Registered user     Discord:[{user.DiscordUsername}]     InGame:[{user.IngameName}]     was not located in the Discord userlist.  Unable to payout boost rewards.");
                    continue;
                }
                
                List<string> userRoles = guildUser.Roles.Select(role => role.Name).ToList();
                for (int roleIndex = userRoles.Count - 1; roleIndex >= 0; roleIndex--)
                {
                    string role = userRoles[roleIndex];
                    if (!role.Contains("Nitro Booster")) continue;
                    Log.Info($"{guildUser.DisplayName} rewarded for Boosting!");
                    CommandsManager.Run(Instance.Config.BoostRewardsCommand.Replace("{user}", user.IngameSteamId.ToString()));
                }
            }

            return Task.CompletedTask;
        }
    }
}