using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord.Bot;
using Qmmands;

namespace Availabot.Commands.TypeParsers
{
    public class FromUntilParametersTypeParser : DiscordTypeParser<FromUntilParameters>
    {
        public override ValueTask<TypeParserResult<FromUntilParameters>> ParseAsync(Parameter parameter, string value, DiscordCommandContext context)
        {
            string[] parameters = value.Split(new[] {" until ", " to "}, StringSplitOptions.RemoveEmptyEntries);
            if(parameters.Length != 2) return Failure("Please provide two valid DateTimes separated by \"until\" or \"to\". (eg. \"2pm until 5:30pm\")");

            string startsValue = parameters[0];
            string expiresValue = parameters[1];

            if (DateTime.TryParse(startsValue, out DateTime starts) &&
                DateTime.TryParse(expiresValue, out DateTime expires))
            {
                starts = starts.ToUniversalTime();
                expires = expires.ToUniversalTime();
                if (starts < DateTime.UtcNow || expires < DateTime.UtcNow) return Failure("Both DateTimes must be in the future.");
                if (starts < expires) return Success(new FromUntilParameters(starts, expires));
                else return Failure("The first DateTime must be earlier than the second DateTime.");
            }
            else return Failure("Please provide two valid DateTimes separated by \"until\" or \"to\". (eg. \"2pm until 5:30pm\")");
        }
    }

    public class FromUntilParameters
    {
        public DateTime Starts { get; set; }
        public DateTime Expires { get; set; }

        public FromUntilParameters(DateTime starts, DateTime expires)
        {
            Starts = starts;
            Expires = expires;
        }
    }
}
