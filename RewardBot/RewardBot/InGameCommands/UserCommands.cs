using System;
using System.Collections.Generic;
using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using RewardBot.Settings;
using VRage.Game.ModAPI;

namespace RewardBot.Commands
{
    [Category("RewardBot")]
    public sealed class RewardBotCommands : CommandModule
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
        public async void LinkAccount(string code)
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
                await Plugin.Save();
                
                Context.Respond($"Your SteamID [{user.IngameSteamId}] has successfully been linked to your Discord account [{user.DiscordUsername}], and are now registered to receive rewards!");
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
        public async void Unlink()
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
                await Plugin.Save();
            }
            else 
            {
                Context.Respond($"Unable to find any record with your SteamID [{Context.Player.SteamUserId}].  If you believe this is in error, please make a ticket.");
            }
        }
        
        [Command("Claim", "Issue all rewards you may have available.")]
        [Permission(MyPromoteLevel.None)]
        public async void ClaimRewards()
        {
            if (Context.Player == null)
            {
                Context.Respond("This command must be run in game.");
                return;
            }

            int count = 0;
            for (int claimIndex = Plugin.Config.Payouts.Count - 1; claimIndex >= 0; claimIndex--)
            {
                if (Plugin.Config.Payouts[claimIndex].SteamID != Context.Player.SteamUserId) continue;
                Payout payout = Plugin.Config.Payouts[claimIndex];
                count++;
                await MainBot.CommandsManager.Run(payout.Command);
                await Plugin.Control.Dispatcher.BeginInvoke((Action) (() => { Plugin.Config.Payouts.Remove(payout); }));
            }

            Context.Respond(count == 0 ? "You have no rewards available to claim at this time." : $"{count} reward(s) have been issued.");

            await Plugin.Save();
        }

        [Command("Claim Item", "Claim your reward by [ID]")]
        [Permission(MyPromoteLevel.None)]
        public async void ClaimItem(int payoutID)
        {
            if (Context.Player == null)
            {
                Context.Respond("This command must be run in game.");
                return;
            }

            bool payoutIssued = false;
            for (int index = Plugin.Config.Payouts.Count - 1; index >= 0; index--)
            {
                Payout payout = Plugin.Config.Payouts[index];
                if (payout.SteamID != Context.Player.SteamUserId) continue;
                if (payout.ID != payoutID) continue;
                
                await MainBot.CommandsManager.Run(payout.Command);
                await Plugin.Control.Dispatcher.BeginInvoke((Action) (() => { Plugin.Config.Payouts.Remove(payout); })); 
                payoutIssued = true;
                break;
            }
            
            Context.Respond(payoutIssued ? "Your reward has been issued." : "No reward with that ID is available to you or no reward with that ID exists.");
            await Plugin.Save();
        }

        [Command("List", "Show all your rewards available to claim.")]
        [Permission(MyPromoteLevel.None)]
        public void ClaimItem()
        {
            StringBuilder listRewards = new StringBuilder();
            listRewards.AppendLine("--- AVAILABLE REWARDS ---");
            int count = 0;
            for (int index = Plugin.Config.Payouts.Count - 1; index >= 0; index--)
            {
                Payout payout = Plugin.Config.Payouts[index];
                if (payout.SteamID != Context.Player.SteamUserId) continue;
                count++;
                listRewards.AppendLine($"[ID: {payout.ID}] [Expires: {payout.ExpiryDate.ToShortDateString()}] [Name: {payout.RewardName}]");
            }

            if (count == 0)
                listRewards.AppendLine("None...");
            Context.Respond(listRewards.ToString());
        }
    }
}
