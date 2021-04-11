using System;
using System.Threading.Tasks;
using Availabot.Extensions;
using Availabot.Services;
using Database.Contexts;
using Disqord.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qmmands;

namespace Availabot.Commands
{
    public class AvailabilityCommands : DiscordGuildModuleBase
    {
        ILogger _logger;
        IDbContextFactory<DatabaseContext> _db;
        AvailabilityService _availability;

        public AvailabilityCommands(ILoggerProvider loggerProvider, IDbContextFactory<DatabaseContext> db, AvailabilityService availability)
        {
            _logger = loggerProvider.CreateLogger("AvailabilityCommands");
            _db = db;
            _availability = availability;
        }

        [Command("for")]
        public async Task Available([Remainder] TimeSpan timespan)
        {
            _logger.LogDebug($"{Context.Author} wants to be available for {timespan}");
            await _availability.MakeUserAvailableAsync(Context.GuildId, Context.Author.Id, timespan);
            await Context.Channel.SendSuccessAsync("Marked as available", $"You'll be marked as available for the next {timespan.ToLongString()}");
        }

        [Command("until")]
        public async Task AvailableUntil([Remainder] DateTime datetime)
        {
            _logger.LogDebug($"{Context.Author} wants to be available until {datetime}");
            TimeSpan timespan = datetime - DateTime.UtcNow;
            await _availability.MakeUserAvailableAsync(Context.GuildId, Context.Author.Id, timespan);
            await Context.Channel.SendSuccessAsync("Marked as available", $"You'll be marked as available for the next {timespan.ToLongString()}");
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
