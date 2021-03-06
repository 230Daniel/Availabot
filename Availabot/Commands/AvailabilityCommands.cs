using System;
using System.Threading.Tasks;
using Availabot.Commands.TypeParsers;
using Availabot.Extensions;
using Availabot.Services;
using Disqord.Bot;
using Microsoft.Extensions.Logging;
using Qmmands;

namespace Availabot.Commands
{
    public class AvailabilityCommands : DiscordGuildModuleBase
    {
        ILogger _logger;
        AvailabilityService _availability;

        public AvailabilityCommands(ILogger<AvailabilityCommands> logger, AvailabilityService availability)
        {
            _logger = logger;
            _availability = availability;
        }

        [Command("for")]
        public async Task Available([Remainder] TimeSpan duration)
        {
            _logger.LogDebug($"{Context.Author} wants to be available for {duration}");
            await _availability.MakeUserAvailableAsync(Context.GuildId, Context.Author.Id, DateTime.UtcNow, DateTime.UtcNow + duration);
            await Context.Channel.SendSuccessAsync("Marked as available", $"You'll be marked as available for the next {duration.ToLongString()}");
        }

        [Command("until")]
        public async Task AvailableUntil([Remainder] DateTime expires)
        {
            _logger.LogDebug($"{Context.Author} wants to be available until {expires}");
            await _availability.MakeUserAvailableAsync(Context.GuildId, Context.Author.Id, DateTime.UtcNow, expires);
            await Context.Channel.SendSuccessAsync("Marked as available", $"You'll be marked as available for {(expires - DateTime.UtcNow).ToLongString()}");
        }

        [Command("from")]
        public async Task FromUntil([Remainder] FromUntilParameters parameters)
        {
            _logger.LogDebug($"{Context.Author} wants to be available from {parameters.Starts} until {parameters.Expires}");
            await _availability.MakeUserAvailableAsync(Context.GuildId, Context.Author.Id, parameters.Starts, parameters.Expires);
            await Context.Channel.SendSuccessAsync("Scheduled availability", $"You'll be marked as available for {(parameters.Expires - parameters.Starts).ToLongString()} starting in {(parameters.Starts - DateTime.UtcNow).ToLongString()}");
        }

        [Command("unavailable")]
        public async Task Unavailable()
        {
            _logger.LogDebug($"{Context.Author} wants to be unavailable");
            await _availability.MakeUserUnavailableAsync(Context.GuildId, Context.Author.Id);
            await Context.Channel.SendSuccessAsync("Marked as unavailable");
        }
    }
}
