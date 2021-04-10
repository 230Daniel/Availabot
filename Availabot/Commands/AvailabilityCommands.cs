using System;
using System.Linq;
using System.Threading.Tasks;
using Disqord;
using Disqord.Bot;
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.Logging;
using Qmmands;

namespace Availabot.Commands
{
    public class AvailabilityCommands : DiscordGuildModuleBase
    {
        ILogger _logger;

        public AvailabilityCommands(ILoggerProvider loggerProvider)
        {
            _logger = loggerProvider.CreateLogger("AvailabilityCommands");
        }
    }
}
