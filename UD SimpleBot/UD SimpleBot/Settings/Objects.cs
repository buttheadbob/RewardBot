using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UD_SimpleBot.Settings
{
    public enum Bot_Status { Online, Offline }

    public sealed class RegisteredUsers
    {
        public string Ingame_Name { get; set; }
        public ulong Ingame_SteamID { get; set; }
        public string Discord_Username { get; set; }
        public ulong Discord_ID { get; set; }
        public SocketGuildUser Discord {get; set;}
        public DateTime Registered { get; set; }
    }
}
