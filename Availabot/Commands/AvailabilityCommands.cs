using System.Linq;
using System.Threading.Tasks;
using Database.Contexts;
using Database.Models;
using Disqord.Bot;
using Microsoft.Extensions.Logging;
using Qmmands;

namespace Availabot.Commands
{
    public class AvailabilityCommands : DiscordGuildModuleBase
    {
        ILogger _logger;
        DatabaseContext _db;

        public AvailabilityCommands(ILoggerProvider loggerProvider, DatabaseContext db)
        {
            _logger = loggerProvider.CreateLogger("AvailabilityCommands");
            _db = db;
        }

        [Command("TestAdd")]
        public async Task TestAdd()
        {
            AvailabilityPeriod period = new AvailabilityPeriod()
            {
                UserId = Context.Author.Id,
                GuildId = Context.GuildId
            };

            await _db.AvailabilityPeriods.AddAsync(period);
            await _db.SaveChangesAsync();

            await Response("Added an availability period.");
        }

        [Command("TestGet")]
        public async Task TestGet()
        {
            IQueryable<AvailabilityPeriod> periods = _db.AvailabilityPeriods.Where(x => x.UserId == Context.Author.Id.RawValue && x.GuildId == Context.GuildId.RawValue);

            await Response($"You have {periods.Count()} availability periods.");
        }
    }
}
