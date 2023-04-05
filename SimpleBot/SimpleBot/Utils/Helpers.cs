using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SimpleBot.Settings;
using static SimpleBot.MainBot;

namespace SimpleBot.Utils
{
    public static class Helper
    {
        // SunsetQuest Method
        public static string RemoveWhiteSpace(string str)
        {
            int len = str.Length;
            char[] src = str.ToCharArray();
            int dstIdx = 0;

            for (int i = 0; i < len; i++) {
                char ch = src[i];

                switch (ch) {

                    case '\u0020': case '\u00A0': case '\u1680': case '\u2000': case '\u2001':
                    case '\u2002': case '\u2003': case '\u2004': case '\u2005': case '\u2006':
                    case '\u2007': case '\u2008': case '\u2009': case '\u200A': case '\u202F':
                    case '\u205F': case '\u3000': case '\u2028': case '\u2029': case '\u0009':
                    case '\u000A': case '\u000B': case '\u000C': case '\u000D': case '\u0085':
                        continue;

                    default:
                        src[dstIdx++] = ch;
                        break;
                }
            }

            return new string(src, 0, dstIdx);
        }
        
        public static Task Payout(bool payAll = false)
        {
            if (!payAll)
            {
                if (Instance.Config.LastPayout.Month == DateTime.Now.Month && Instance.Config.LastPayout.Day == DateTime.Now.Day)
                    return Task.CompletedTask;

                for (int index = Instance.Config.RegisteredUsers.Count - 1; index >= 0; index--)
                {
                    RegisteredUsers registeredUser = Instance.Config.RegisteredUsers[index];

                    if (registeredUser.LastPayout.Month != DateTime.Now.Month || registeredUser.LastPayout.Day < DateTime.Now.Day)
                    {
                        List<string> commandsToSend = new List<string>();

                        for (int i = Instance.Config.RewardCommands.Count - 1; i >= 0; i--)
                        {
                            if (Instance.Config.RewardCommands[i] == null) continue;
                            Settings.Commands command = Instance.Config.RewardCommands[i];
                            string commandToSend = command.Command.Replace("{SteamID}", registeredUser.IngameSteamId.ToString());
                            commandsToSend.Add(commandToSend.Replace("{Username}", registeredUser.IngameName));
                        }

                        Instance.Config.Payouts.Add(new Payout()
                        {
                            IngameName = registeredUser.IngameName,
                            SteamID = registeredUser.IngameSteamId,
                            PaymentDate = DateTime.Now,
                            ExpiryDate = DateTime.Now + TimeSpan.FromDays(Instance.Config.PayoutExpiry),
                            Commands = commandsToSend,
                            DiscordId = registeredUser.DiscordId,
                            DiscordName = registeredUser.DiscordUsername
                        });
                        
                        registeredUser.LastPayout = DateTime.Now;
                    }
                }
                Instance.Save();
                return Task.CompletedTask;
            }
            
            for (int index = Instance.Config.RegisteredUsers.Count - 1; index >= 0; index--)
            {
                RegisteredUsers registeredUser = Instance.Config.RegisteredUsers[index];

                List<string> commandsToSend = new List<string>();

                for (int i = Instance.Config.RewardCommands.Count - 1; i >= 0; i--)
                {
                    if (Instance.Config.RewardCommands[i] == null) continue;
                    Settings.Commands command = Instance.Config.RewardCommands[i];
                    string commandToSend = command.Command.Replace("{SteamID}", registeredUser.IngameSteamId.ToString());
                    commandsToSend.Add(commandToSend.Replace("{Username}", registeredUser.IngameName));
                }

                Instance.Config.Payouts.Add(new Payout()
                {
                    IngameName = registeredUser.IngameName,
                    SteamID = registeredUser.IngameSteamId,
                    PaymentDate = DateTime.Now,
                    ExpiryDate = DateTime.Now + TimeSpan.FromDays(Instance.Config.PayoutExpiry),
                    Commands = commandsToSend,
                    DiscordId = registeredUser.DiscordId,
                    DiscordName = registeredUser.DiscordUsername
                });
                        
                registeredUser.LastPayout = DateTime.Now;
            }
            Instance.Save();
            return Task.CompletedTask;
        }
    }
}