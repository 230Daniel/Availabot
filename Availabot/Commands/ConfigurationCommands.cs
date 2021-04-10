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
using Availabot.Services;
using Availabot.Utils;
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;

namespace Availabot.Commands
{
    public class ConfigurationCommands : DiscordGuildModuleBase
    {
        ILogger _logger;
        IDbContextFactory<DatabaseContext> _db;
        AvailabilityService _availability;

        public ConfigurationCommands(ILoggerProvider loggerProvider, IDbContextFactory<DatabaseContext> db, AvailabilityService availability)
        {
            _logger = loggerProvider.CreateLogger("ConfigurationCommands");
            _db = db;
            _availability = availability;
        }

        [Command("setup"), RequireAuthorGuildPermissions(Permission.ManageGuild)]
        public async Task Setup()
        {
            using DatabaseContext db = _db.CreateDbContext();
            GuildConfiguration config = db.GetGuildConfiguration(Context.GuildId);

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
                if(Mention.TryParseChannel(response.Message.Content, out Snowflake channelId) && 
                   Context.Guild.Channels.TryGetValue(channelId, out IGuildChannel guildChannel) && 
                   guildChannel is ITextChannel) config.ChannelId = channelId;
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
                if(Mention.TryParseRole(response.Message.Content, out Snowflake roleId) && 
                   Context.Guild.Roles.TryGetValue(roleId, out _)) config.RoleId = roleId;
                else await Response("Invalid input, please try again.");
            }

            ITextChannel channel = Context.Guild.Channels.First(x => x.Key == config.ChannelId).Value as ITextChannel;
            IUserMessage message = await channel.SendMessageAsync(new LocalMessageBuilder()
                .WithContent("This message will be modified shortly...")
                .WithMentions(LocalMentionsBuilder.None)
                .Build());

            foreach (IEmoji emoji in Constants.NumberEmojis.Take(5))
                await message.AddReactionAsync(emoji);
            await message.AddReactionAsync(Constants.XEmoji);

            config.MessageId = message.Id;

            db.GuildConfigurations.Update(config);
            await db.SaveChangesAsync();

            await _availability.UpdateGuildMessageAsync(Context.GuildId);

            await Context.Channel.SendSuccessAsync("Setup completed");
        }
    }
}
