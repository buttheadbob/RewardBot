using System;

namespace RewardBot.Settings
{
    public enum BotStatusEnum { Online, Offline, Connecting, Disconnecting }

    public sealed class RegisteredUsers
    {
        public string IngameName { get; set; }
        public ulong IngameSteamId { get; set; }
        public string DiscordUsername { get; set; }
        public ulong DiscordId { get; set; }
        public DateTime Registered { get; set; }
        public DateTime LastPayout { get; set; }
    }

    public sealed class LinkRequest
    {
        public string Code { get; set; }
        public ulong DiscordId { get; set; }
        public string DiscordUsername { get; set; }
        public DateTime Created { get; set; }
    }

    public sealed class Reward
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Command { get; set; }
        public string CommandRole { get; set; }
        public int ExpiresInDays { get; set; }
        public string DaysToPay { get; set; }
        public DateTime LastRun { get; set; }
    }

    public sealed class Payout
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string RewardName { get; set; }
        public ulong SteamID { get; set; }
        public string IngameName { get; set; }
        public string DiscordName { get; set; }
        public string Command { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public ulong DiscordId { get; set; }
        public string DaysUntilExpired => (ExpiryDate - PaymentDate).Days.ToString();

        public bool ChangeDaysUntilExpire(int days, out string error)
        {
            error = "";
            if (days == 0)
            {
                error = "Cannot use 0 for expiry value, this would expire now.";
                return false;
            }
            
            ExpiryDate = DateTime.Now.AddDays(days);

            return true;
        } 
    }
    
}


