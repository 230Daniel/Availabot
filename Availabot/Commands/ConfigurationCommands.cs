using System;
using System.Linq;
using System.Threading.Tasks;
using Database.Contexts;
using Database.Models;
using Disqord;
using Disqord.Bot;
using Microsoft.Extensions.Logging;
using Qmmands;
using Availabot.Extensions;
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;

namespace Availabot.Commands
{
    public class ConfigurationCommands : DiscordGuildModuleBase
    {
        ILogger _logger;
        DatabaseContext _db;

        public ConfigurationCommands(ILoggerProvider loggerProvider, DatabaseContext db)
        {
            _logger = loggerProvider.CreateLogger("ConfigurationCommands");
            _db = db;
        }

        [Command("Setup"), RequireAuthorGuildPermissions(Permission.ManageGuild)]
        public async Task Setup()
        {
            GuildConfiguration config = _db.GetGuildConfiguration(Context.GuildId);

            await Context.Channel.SendInfoAsync("Starting setup");

            await Response("Which channel should Availabot be used in? (#mention channel)");
            config.ChannelId = 0;
            while (config.ChannelId == 0)
            {
                MessageReceivedEventArgs response = await Context.WaitForMessageAsync(x => x.Member.Id == Context.Author.Id, TimeSpan.FromMinutes(1));
                if (response is null)
                {
                    await Response("Setup cancelled.");
                    return;
                }
                if(Mention.TryParseChannel(response.Message.Content, out Snowflake channelId)) config.ChannelId = channelId;
                else await Response("Invalid input, please try again.");
            }

            await Response("Which role should be used to label available users? (@mention role)");
            config.RoleId = 0;
            while (config.RoleId == 0)
            {
                MessageReceivedEventArgs response = await Context.WaitForMessageAsync(x => x.Member.Id == Context.Author.Id, TimeSpan.FromMinutes(1));
                if (response is null)
                {
                    await Response("Setup cancelled.");
                    return;
                }
                if(Mention.TryParseRole(response.Message.Content, out Snowflake roleId)) config.RoleId = roleId;
                else await Response("Invalid input, please try again.");
            }

            _db.GuildConfigurations.Update(config);
            await _db.SaveChangesAsync();

            await Context.Channel.SendSuccessAsync("Setup completed");
        }
    }
}
