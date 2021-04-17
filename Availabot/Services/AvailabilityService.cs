using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace Availabot.Services
{
    public class AvailabilityService
    {
        ILogger<AvailabilityService> _logger;
        IConfiguration _configuration;
        IGatewayClient _gateway;
        IRestClient _rest;
        IDbContextFactory<DatabaseContext> _db;
        List<((ulong, ulong), System.Threading.Timer)> _scheduledPeriodChanges;
        Timer _updateAllGuildMessagesTimer;
        Dictionary<ulong, string> _guildMessageCache;

        public AvailabilityService(ILogger<AvailabilityService> logger, IConfiguration configuration, IGatewayClient gateway, IRestClient rest, IDbContextFactory<DatabaseContext> db)
        {
            _logger = logger;
            _configuration = configuration;
            _gateway = gateway;
            _rest = rest;
            _db = db;
            _scheduledPeriodChanges = new List<((ulong, ulong), System.Threading.Timer)>();
            _updateAllGuildMessagesTimer = new Timer(10000);
            _updateAllGuildMessagesTimer.Elapsed += UpdateAllGuildMessagesTimer_Elapsed;
            _guildMessageCache = new Dictionary<ulong, string>();
        }

        public async Task StartAsync()
        {
            _updateAllGuildMessagesTimer.Start();

            await using DatabaseContext db = _db.CreateDbContext();
            List<AvailabilityPeriod> periods = db.GetUniqueAvailabilityPeriods();

            foreach (AvailabilityPeriod period in periods)
                SchedulePeriodChanges(period);

            // TODO: Check which users have the role but aren't available and take it away
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
                    await MakeUserAvailableAsync(e.GuildId.Value, e.UserId, DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromHours(hours));
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

        public async Task MakeUserAvailableAsync(ulong guildId, ulong userId, DateTime starts, DateTime expires)
        {
            await using DatabaseContext db = _db.CreateDbContext();
            AvailabilityPeriod period = await db.AvailabilityPeriods.FirstOrDefaultAsync(x => x.GuildId == guildId && x.UserId == userId);

            if (period is null)
            {
                period = new AvailabilityPeriod
                {
                    GuildId = guildId,
                    UserId = userId,
                    Starts = starts,
                    Expires = expires
                };
                await db.AvailabilityPeriods.AddAsync(period);
            }
            else
            {
                period.Starts = starts;
                period.Expires = expires;
                db.AvailabilityPeriods.Update(period);
            }
            
            await db.SaveChangesAsync();
            await UpdateGuildMessageAsync(guildId);
            if(period.Starts <= DateTime.UtcNow) await GrantUserAvailableRoleAsync(guildId, userId);
            SchedulePeriodChanges(period);
        }

        public async Task MakeUserUnavailableAsync(ulong guildId, ulong userId)
        {
            await using DatabaseContext db = _db.CreateDbContext();
            IQueryable<AvailabilityPeriod> periods = db.AvailabilityPeriods.Where(x => x.GuildId == guildId && x.UserId == userId);

            foreach (AvailabilityPeriod period in periods)
                db.AvailabilityPeriods.Remove(period);
            
            await db.SaveChangesAsync();
            await UpdateGuildMessageAsync(guildId);
            await RevokeUserAvailableRoleAsync(guildId, userId);
        }

        void SchedulePeriodChanges(AvailabilityPeriod period)
        {
            if (_scheduledPeriodChanges.Any(x => x.Item1 == (period.GuildId, period.UserId)))
            {
                foreach(((ulong, ulong), System.Threading.Timer) change in _scheduledPeriodChanges.Where(x => x.Item1 == (period.GuildId, period.UserId)))
                    change.Item2.Dispose();
                _scheduledPeriodChanges.RemoveAll(x => x.Item1 == (period.GuildId, period.UserId));
            }

            if(period.Starts <= DateTime.UtcNow)
            {
                // Schedule unavailable
                _ = GrantUserAvailableRoleAsync(period.GuildId, period.UserId);
                TimeSpan delay = period.Expires - DateTime.UtcNow;
                if (delay <= TimeSpan.Zero)
                {
                    _ = MakeUserUnavailableAsync(period.GuildId, period.UserId);
                }
                else
                {
                    System.Threading.Timer timer = new System.Threading.Timer(x =>
                    {
                        _ = MakeUserUnavailableAsync(period.GuildId, period.UserId);
                    }, null, delay, Timeout.InfiniteTimeSpan);

                    _scheduledPeriodChanges.Add(((period.GuildId, period.UserId), timer));
                }
            }
            else
            {
                // Schedule available
                _ = RevokeUserAvailableRoleAsync(period.GuildId, period.UserId);
                TimeSpan delay = period.Starts - DateTime.UtcNow;
                if (delay <= TimeSpan.Zero)
                {
                    _ = MakeUserAvailableAsync(period.GuildId, period.UserId, period.Starts, period.Expires);
                }
                else
                {
                    System.Threading.Timer timer = new System.Threading.Timer(x =>
                    {
                        _ = MakeUserAvailableAsync(period.GuildId, period.UserId, period.Starts, period.Expires);
                    }, null, delay, Timeout.InfiniteTimeSpan);

                    _scheduledPeriodChanges.Add(((period.GuildId, period.UserId), timer));
                }
            }
        }

        async Task GrantUserAvailableRoleAsync(ulong guildId, ulong userId)
        {
            await using DatabaseContext db = _db.CreateDbContext();
            GuildConfiguration config = db.GetGuildConfiguration(guildId);

            await _rest.GrantRoleAsync(guildId, userId, config.RoleId);
        }

        async Task RevokeUserAvailableRoleAsync(ulong guildId, ulong userId)
        {
            await using DatabaseContext db = _db.CreateDbContext();
            GuildConfiguration config = db.GetGuildConfiguration(guildId);

            await _rest.RevokeRoleAsync(guildId, userId, config.RoleId);
        }

        void UpdateAllGuildMessagesTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            using DatabaseContext db = _db.CreateDbContext();
            foreach (ulong guildId in db.GuildConfigurations.Select(x => x.GuildId))
            {
                _ = UpdateGuildMessageAsync(guildId);
            }
        }

        public async Task UpdateGuildMessageAsync(ulong guildId)
        {
            try
            {
                await using DatabaseContext db = _db.CreateDbContext();
                GuildConfiguration config = db.GetGuildConfiguration(guildId);
                List<AvailabilityPeriod> periods = db.GetFilteredAvailabilityPeriods(guildId);

                if(!(_gateway.GetChannel(guildId, config.ChannelId) is ITextChannel channel)) return;

                if (!_guildMessageCache.TryGetValue(config.MessageId, out string previousContent))
                {
                    IUserMessage message = channel.FetchMessageAsync(config.MessageId).GetAwaiter().GetResult() as IUserMessage;
                    _guildMessageCache.TryAdd(config.MessageId, message.Content);
                }

                string content = "⠀";

                if (periods.Count(x => x.Starts <= DateTime.UtcNow) > 0)
                {
                    content += "\n__Currently Available__\n\n";
                    foreach (AvailabilityPeriod period in periods.Where(x => x.Starts <= DateTime.UtcNow).OrderByDescending(x => x.Expires))
                        content +=
                            $"{Mention.User(period.UserId)} for {(period.Expires - DateTime.UtcNow).ToLongString()}\n";
                }
                else
                    content += "\nNobody is available at the moment :(\n";

                if (periods.Count(x => x.Starts > DateTime.UtcNow) > 0)
                {
                    content += "\n__Becoming Available__\n\n";
                    foreach (AvailabilityPeriod period in periods.Where(x => x.Starts > DateTime.UtcNow).OrderBy(x => x.Starts))
                        content +=
                            $"{Mention.User(period.UserId)} in {(period.Starts - DateTime.UtcNow).ToLongString()}\n";
                }

                string prefix = _configuration.GetValue<string>("prefix");
                content += $"\n> {prefix}for [timespan]\n" +
                           $"> {prefix}until [datetime]\n" +
                           $"> {prefix}from [datetime] until [datetime]\n" +
                           $"> {prefix}unavailable\n\n" +
                           "Or press a number button below to set yourself as available for the next x hours";

                if(previousContent == content) return;

                await channel.ModifyMessageAsync(config.MessageId, x => x.Content = content);

                _guildMessageCache.Remove(config.MessageId);
                _guildMessageCache.Add(config.MessageId, content);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception thrown updating guild message");
            }
        }
    }
}
