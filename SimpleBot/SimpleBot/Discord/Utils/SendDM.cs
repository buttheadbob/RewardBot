using System.Threading.Tasks;
using Discord;
using Discord.Net;

namespace SimpleBot.DiscordBot.Utils
{
    public class UserUtils
    {
        public async Task<string> SendDirectMessage(IUser user, string message)
        {
            try
            {
                await user.SendMessageAsync(message);
                return "Message sent successfully.";
            }
            catch (HttpException error)
            {
                DiscordErrorCode? errCode = error.DiscordCode;
                string errRespone = string.Empty;
                
                switch (errCode)
                {
                    case null:
                        errRespone = "Unable to send message, reason unknown.";
                        break;
                    
                    case DiscordErrorCode.CannotSendMessageToUser:
                        errRespone = $"Cannot send message, user has either blocked the bot or not allowed messages from the public.";
                        break;
                    
                    case DiscordErrorCode.CannotSendExplicitContent:
                        errRespone = "Explicit content detected in the message by Discord, cannot send.";
                        break;
                    
                    default:
                        errRespone = error.Message;
                        break;
                }

                return errRespone;
            }
        }
    }
}