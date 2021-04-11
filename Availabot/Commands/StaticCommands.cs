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
                "available [time] - Set yourself as available for that time\n" +
                "unavailable - Set yourself as unavailable");
        }
    }
}
