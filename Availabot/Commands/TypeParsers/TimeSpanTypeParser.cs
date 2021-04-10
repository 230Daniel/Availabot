using System;
using System.Threading.Tasks;
using Disqord.Bot;
using Qmmands;
using TimeSpanParserUtil;

namespace Availabot.Commands.TypeParsers
{
    class TimeSpanTypeParser : DiscordTypeParser<TimeSpan>
    {
        public override ValueTask<TypeParserResult<TimeSpan>> ParseAsync(Parameter parameter, string value, DiscordCommandContext context)
        {
            return TimeSpanParser.TryParse(value, out TimeSpan timeSpan) ? Success(timeSpan) : Failure("Invalid TimeSpan.");
        }
    }
}
