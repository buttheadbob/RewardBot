using System.Collections.Generic;
using System.Threading.Tasks;
using Torch.API.Managers;
using Torch.Commands;

namespace SimpleBot.Utils
{
    public class CommandsManager
    {
        private CommandManager _manager;

        private Task<bool> GetTorchCommandManager()
        {
            if (_manager != null)
                return Task.FromResult(true);
            
            if (MainBot.Instance.Torch.CurrentSession.Managers.GetManager<CommandManager>() == null)
                return Task.FromResult(false);
            
            _manager = MainBot.Instance.Torch.CurrentSession.Managers.GetManager<CommandManager>();
            return Task.FromResult(true);
        }
        
        public async Task Run(string command)
        {
            if (await GetTorchCommandManager() == false)
                return;
                
            if (_manager == null)
                MainBot.Log.Error($"Command Manager unable to run command [{command}].  Torch has no active command manager.");
            
            if (!MainBot.Instance.WorldOnline)
                MainBot.Log.Error($"Command Manager unable to run command [{command}].  The server is offline.");
            
            _manager?.HandleCommandFromServer(command);
        }

        public async Task RunSlow(List<string> commands)
        {
            // When a player has a list of 3 or more commands to run, they will run then here.  This prevents
            // and possible server bogging if the commands require heavy processing.  This only blocks the current
            // task in awaitable state, so everything else can still run.
            
            if (await GetTorchCommandManager() == false)
                MainBot.Log.Error($"Command Manager unable to run (slow) commands.  Torch has no active command manager.");
            
            if (!MainBot.Instance.WorldOnline)
                MainBot.Log.Error($"Command Manager unable to run (slow) command.  The server is offline.");

            foreach (string command in commands)
            {
                _manager?.HandleCommandFromServer(command);
                await Task.Delay(5000);
            }
        }
    }
}