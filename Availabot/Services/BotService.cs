using System.Threading;
using System.Threading.Tasks;
using Disqord;
using Disqord.Hosting;
using Microsoft.Extensions.Logging;

namespace Availabot.Services
{
    public class BotService : DiscordClientService
    {
        AvailabilityService _availability;

        public BotService(
            ILogger<BotService> logger,
            DiscordClientBase client,
            AvailabilityService availability)
            : base(logger, client)
        {
            _availability = availability;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Client.WaitUntilReadyAsync(stoppingToken);

            await _availability.StartAsync();
            Client.ReactionAdded += _availability.HandleReactionAsync;
        }
    }
}
