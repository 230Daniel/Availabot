using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Availabot.Extensions;
using Availabot.Utils;
using Database.Contexts;
using Database.Models;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.Extensions.Logging;

namespace Availabot.Services
{
    public class AvailabilityService
    {
        ILogger<AvailabilityService> _logger;
        IGatewayClient _client;
        DatabaseContext _db;

        public AvailabilityService(ILogger<AvailabilityService> logger, IGatewayClient client, DatabaseContext db)
        {
            _logger = logger;
            _client = client;
            _db = db;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("Started service");
        }

        public async Task HandleReactionAsync(object sender, ReactionAddedEventArgs e)
        {
            if(e.Member.IsBot || !e.GuildId.HasValue) return;

            GuildConfiguration config = _db.GetGuildConfiguration(e.GuildId.Value);

            if(e.ChannelId == config.ChannelId && e.MessageId == config.MessageId)
            {
                if (Constants.NumberEmojis.Take(5).Contains(e.Emoji))
                {
                    int hours = Array.IndexOf(Constants.NumberEmojis, e.Emoji) + 1;
                    _logger.LogInformation($"{e.Member} wants to be available for {hours} hours");
                }
                else
                {
                    await e.Message.RemoveReactionAsync(e.Emoji, e.UserId);
                }
            }
        }

        public async Task UpdateGuildMessageAsync(ulong guildId)
        {
            GuildConfiguration config = _db.GetGuildConfiguration(guildId);

            IGuild guild = _client.GetGuild(guildId);
            ITextChannel channel = _client.GetChannel(guildId, config.ChannelId) as ITextChannel;
            IUserMessage message = await channel.FetchMessageAsync(config.MessageId) as IUserMessage;

            string content = "edited";

            await message.ModifyAsync(x => x.Content = content);
        }
    }
}
