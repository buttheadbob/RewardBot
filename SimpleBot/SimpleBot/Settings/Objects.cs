using System;
using System.Collections.Generic;

namespace SimpleBot.Settings
{
    public enum BotStatus { Online, Offline }

    public sealed class RegisteredUsers
    {
        public string IngameName { get; set; }
        public ulong IngameSteamId { get; set; }
        public string DiscordUsername { get; set; }
        public ulong DiscordId { get; set; }
        public DateTime Registered { get; set; }
        public DateTime LastPayout { get; set; }
    }

    public sealed class Members
    {
        public string Username { get; set; }
        public string Nickname { get; set; }
        public ulong UserId { get; set; }
    }

    public sealed class LinkRequest
    {
        public string Code { get; set; }
        public ulong DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public DateTime Created { get; set; }
    }

    public sealed class Commands
    {
        public string Name { get; set; }
        public string Command { get; set; }
    }

    public sealed class Payout
    {
        public ulong SteamID { get; set; }
        public string IngameName { get; set; }
        public List<string> Commands { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public ulong DiscordId { get; set; }
    }
}
