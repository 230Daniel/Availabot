using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;
using Availabot.Extensions;
using Availabot.Utils;
using Database.Contexts;
using Database.Models;
using Disqord;
using Disqord.Gateway;
using Disqord.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Availabot.Services
{
    public class AvailabilityService
    {
        ILogger<AvailabilityService> _logger;
        IGatewayClient _gateway;
        IRestClient _rest;
        IDbContextFactory<DatabaseContext> _db;

        Timer _updateAllGuildMessagesTimer;

        public AvailabilityService(ILogger<AvailabilityService> logger, IGatewayClient gateway, IRestClient rest, IDbContextFactory<DatabaseContext> db)
        {
            _logger = logger;
            _gateway = gateway;
            _rest = rest;
            _db = db;
            _updateAllGuildMessagesTimer = new Timer(10000);
            _updateAllGuildMessagesTimer.Elapsed += UpdateAllGuildMessagesTimer_Elapsed;
        }

        public async Task StartAsync()
        {
            _updateAllGuildMessagesTimer.Start();
        }

        public async Task HandleReactionAsync(object sender, ReactionAddedEventArgs e)
        {
            if(e.Member.IsBot || !e.GuildId.HasValue) return;
            
            using DatabaseContext db = _db.CreateDbContext();
            GuildConfiguration config = db.GetGuildConfiguration(e.GuildId.Value);
            ITextChannel channel = _gateway.GetChannel(config.GuildId, config.ChannelId) as ITextChannel;

            if(e.ChannelId == config.ChannelId && e.MessageId == config.MessageId)
            {
                if (Constants.NumberEmojis.Take(5).Contains(e.Emoji))
                {
                    int hours = Array.IndexOf(Constants.NumberEmojis, e.Emoji) + 1;
                    _logger.LogDebug($"{e.Member} wants to be available for {hours} hours");
                    await MakeUserAvailableAsync(e.GuildId.Value, e.UserId, TimeSpan.FromHours(hours));
                    await channel.SendSuccessAsync("Marked as available", $"You'll be marked as available for the next {hours} hour{(hours == 1 ? "" : "s")}", raw: Mention.User(e.UserId));
                }
                else if (e.Emoji.Equals(Constants.XEmoji))
                {
                    _logger.LogDebug($"{e.Member} wants to be unavailable");
                    await MakeUserUnavailableAsync(e.GuildId.Value, e.UserId);
                    await channel.SendSuccessAsync("Marked as unavailable", raw: Mention.User(e.UserId));
                }
                await _rest.RemoveReactionAsync(config.ChannelId, config.MessageId, e.Emoji, e.UserId);
            }
        }

        private void UpdateAllGuildMessagesTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            using DatabaseContext db = _db.CreateDbContext();
            foreach (ulong guildId in db.GuildConfigurations.Select(x => x.GuildId))
            {
                _ = UpdateGuildMessageAsync(guildId);
            }
        }

        public async Task MakeUserAvailableAsync(ulong guildId, ulong userId, TimeSpan time)
        {
            await using DatabaseContext db = _db.CreateDbContext();
            AvailabilityPeriod period = await db.AvailabilityPeriods.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);

            if (period is null)
            {
                period = new AvailabilityPeriod
                {
                    GuildId = guildId,
                    UserId = userId,
                    Expires = DateTime.UtcNow + time
                };
                await db.AvailabilityPeriods.AddAsync(period);
            }
            else
            {
                period.Expires = DateTime.UtcNow + time;
                db.AvailabilityPeriods.Update(period);
            }
            
            await db.SaveChangesAsync();
            await UpdateGuildMessageAsync(guildId);
        }

        public async Task MakeUserUnavailableAsync(ulong guildId, ulong userId)
        {
            await using DatabaseContext db = _db.CreateDbContext();
            IQueryable<AvailabilityPeriod> periods = db.AvailabilityPeriods.Where(x => x.GuildId == guildId && x.UserId == userId);

            foreach (AvailabilityPeriod period in periods)
                db.AvailabilityPeriods.Remove(period);
            
            await db.SaveChangesAsync();
            await UpdateGuildMessageAsync(guildId);
        }

        public async Task UpdateGuildMessageAsync(ulong guildId)
        {
            try
            {
                await using DatabaseContext db = _db.CreateDbContext();
                GuildConfiguration config = db.GetGuildConfiguration(guildId);
                List<AvailabilityPeriod> periods = db.GetFilteredAvailabilityPeriods(guildId);

                IGuild guild = _gateway.GetGuild(guildId);
                ITextChannel channel = _gateway.GetChannel(guildId, config.ChannelId) as ITextChannel;
                IUserMessage message = await channel.FetchMessageAsync(config.MessageId) as IUserMessage;

                string content = "⠀\n__Available__\n\n";

                if (periods.Count > 0)
                {
                    foreach (AvailabilityPeriod period in periods)
                        content +=
                            $"{Mention.User(period.UserId)} for {(period.Expires - DateTime.UtcNow).ToLongString()}\n";
                }
                else
                    content += "Nobody is available at the moment :(\n";

                content += "\n*Use !available [time] or !unavailable to set your status\n" +
                           "... or press a number button below to set yourself as available for the next x hours*\n";

                await message.ModifyAsync(x => x.Content = content);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception thrown updating guild message");
            }
        }
    }
}
