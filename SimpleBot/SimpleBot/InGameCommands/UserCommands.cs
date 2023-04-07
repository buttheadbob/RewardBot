using System;
using System.Collections.Generic;
using Torch.Commands;
using Torch.Commands.Permissions;
using SimpleBot.Settings;
using VRage.Game.ModAPI;

namespace SimpleBot.Commands
{
    [Category("RewardBot")]
    public sealed class SimpleBotCommands : CommandModule
    {
        private MainBot Plugin => (MainBot)Context.Plugin;
        
        [Command("MySteamID", "Returns your SteamID.")]
        [Permission(MyPromoteLevel.None)]
        public void GiveSteamId()
        {
            if (Context.Player == null)
            {
                Context.Respond("This command must be run in game.");
                return;
            }
            
            Context.Respond(Context.Player.SteamUserId.ToString());
        }
        
        [Command("Link", "Links your Steam account to your Discord account using a special code you received in Discord.  Use -> !RewardBot Link <code>")]
        [Permission(MyPromoteLevel.None)]
        public void LinkAccount(string code)
        {
            if (Context.Player == null)
            {
                Context.Respond("This command must be run in game.");
                return;
            }
            
            if (string.IsNullOrEmpty(code) || string.IsNullOrWhiteSpace(code))
                Context.Respond("Invalid Code, did you enter the code correctly?");
            bool foundCode = false;
            for (int index = Plugin.Config.LinkRequests.Count - 1; index >= 0; index--)
            {
                LinkRequest request = Plugin.Config.LinkRequests[index];
                if (request.Code != code) continue;
                RegisteredUsers user = new RegisteredUsers()
                {
                    DiscordId = Plugin.Config.LinkRequests[index].DiscordId,
                    IngameName = Context.Player.DisplayName,
                    DiscordUsername = Plugin.Config.LinkRequests[index].DiscordUsername,
                    IngameSteamId = Context.Player.SteamUserId,
                    Registered = DateTime.Now
                };
                Plugin.Config.RegisteredUsers.Add(user);
                Plugin.Config.LinkRequests.Remove(request);
                Plugin.Save();
                
                Context.Respond($"Your SteamID [{user.IngameSteamId}] has successfully been linked to your Discord account [{user.DiscordUsername}], and are now registered to receive Boost Rewards if your boosting the server!");
                foundCode = true;
                break;
            }

            if (!foundCode)
            {
                Context.Respond("Invalid Code, did you enter it correctly?  Codes are only valid for 24 hours.");
            }
        }
        
        [Command("Unlink", "This will remove any connection between your SteamID and Discord account on UpsideDown.  No information will be retained.  You will also no longer be eligible to receive some Rewards if qualified.")]
        [Permission(MyPromoteLevel.None)]
        public void Unlink()
        {
            if (Context.Player == null)
            {
                Context.Respond("This command must be run in game.");
                return;
            }
            
            bool foundId = false;
            RegisteredUsers foundUser = new RegisteredUsers();
            for (int index = Plugin.Config.RegisteredUsers.Count - 1; index >= 0; index--)
            {
                RegisteredUsers registeredUser = Plugin.Config.RegisteredUsers[index];
                if (registeredUser.IngameSteamId != Context.Player.SteamUserId) continue;
                foundUser = registeredUser;
                foundId = true;
            }

            if (foundId)
            {
                Plugin.Config.RegisteredUsers.Remove(foundUser);
                Context.Respond($"Your SteamId is no longer linked to your Discord.  You are no longer able to receive any rewards that require this connection.");
                Plugin.Save();
            }
            else 
            {
                Context.Respond($"Unable to find any record with your SteamID [{Context.Player.SteamUserId}].  If you believe this is in error, please make a ticket.");
            }
        }
        
        [Command("Claim", "Issue any rewards you may have available.")]
        [Permission(MyPromoteLevel.None)]
        public async void ClaimRewards()
        {
            if (Context.Player == null)
            {
                Context.Respond("This command must be run in game.");
                return;
            }

            List<string> commands = new List<string>();
            for (int index = MainBot.Instance.Config.Payouts.Count - 1; index >= 0; index--)
            {
                if (MainBot.Instance.Config.Payouts[index].SteamID == Context.Player.SteamUserId)
                {
                    
                }
            }

            if (commands.Count > 0)
            {
                Context.Respond("You have no rewards to claim at this time.");
                return;
            }
            
            Context.Respond($"You have {commands.Count} rewards being delivered.");

            if (commands.Count > 8)
            {
                await MainBot.CommandsManager.RunSlow(commands);
                Plugin.Save();
                return;
            }

            foreach (string command in commands)
            {
                await MainBot.CommandsManager.Run(command);
                Plugin.Save();
            }
        }
    }
}
