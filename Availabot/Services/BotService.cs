using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Hosting;
using Microsoft.Extensions.Logging;

namespace Availabot.Services
{
    public class BotService : DiscordClientService
    {
        public BotService(
            ILogger<BotService> logger,
            DiscordClientBase client)
            : base(logger, client)
        { }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitUntilReadyAsync(stoppingToken);
        }
    }
}
