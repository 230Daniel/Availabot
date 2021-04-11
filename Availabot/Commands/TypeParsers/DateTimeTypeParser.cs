using System;
using System.Threading.Tasks;
using Disqord.Bot;
using Qmmands;

namespace Availabot.Commands.TypeParsers
{
    class DateTimeTypeParser : DiscordTypeParser<DateTime>
    {
        public override ValueTask<TypeParserResult<DateTime>> ParseAsync(Parameter parameter, string value, DiscordCommandContext context)
        {
            if (DateTime.TryParse(value, out DateTime datetime))
            {
                datetime = datetime.ToUniversalTime();
                return datetime > DateTime.UtcNow ? 
                    Success(datetime) : 
                    Failure("The DateTime must be in the future.");
            }
            return Failure("Invalid DateTime.");
        }
    }
}
