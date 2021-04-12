using System.Threading.Tasks;
using Availabot.Extensions;
using Disqord.Bot;
using Qmmands;

namespace Availabot.Commands
{
    public class StaticCommands : DiscordGuildModuleBase
    {
        [Command("help")]
        public async Task Help()
        {
            await Context.Channel.SendInfoAsync("Availabot",
                "help - Show this list\n" +
                "setup - Set up the bot\n" +
                "for [amount of time] - Set yourself as available for an amount of time\n" +
                "until [time] - Set yourself as available until that time\n" +
                "from [time] to [time] - Set yourself as available between two times\n" +
                "unavailable - Set yourself as unavailable");
        }
    }
}
