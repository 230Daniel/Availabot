using System;
using System.Collections.Generic;
using System.Linq;
using Database.Contexts;
using Database.Models;

namespace Availabot.Extensions
{
    static class DatabaseContextExtensions
    {
        public static GuildConfiguration GetGuildConfiguration(this DatabaseContext db, ulong guildId)
        {
            GuildConfiguration config = db.GuildConfigurations.FirstOrDefault(x => x.GuildId == guildId);
            if (config is null)
            {
                config = new GuildConfiguration(guildId);
                db.GuildConfigurations.Add(config);
                db.SaveChanges();
            }
            return config;
        }

        public static List<AvailabilityPeriod> GetFilteredAvailabilityPeriods(this DatabaseContext db, ulong guildId)
        {
            List<AvailabilityPeriod> periods = db.AvailabilityPeriods.Where(x => x.GuildId == guildId && x.Expires > DateTime.UtcNow).ToList();
            List<AvailabilityPeriod> filteredPeriods = new List<AvailabilityPeriod>();

            foreach (AvailabilityPeriod period in periods)
            {
                if(filteredPeriods.Any(x => x.UserId == period.UserId))
                {
                    AvailabilityPeriod oldPeriod = filteredPeriods.First(x => x.UserId == period.UserId);
                    if (period.Expires > oldPeriod.Expires)
                    {
                        filteredPeriods.Remove(oldPeriod);
                        filteredPeriods.Add(period);
                    }
                }
                else
                {
                    filteredPeriods.Add(period);
                }
            }

            return filteredPeriods;
        }

        public static List<AvailabilityPeriod> GetUniqueAvailabilityPeriods(this DatabaseContext db)
        {
            List<AvailabilityPeriod> periods = db.AvailabilityPeriods.ToList();
            List<AvailabilityPeriod> filteredPeriods = new List<AvailabilityPeriod>();

            foreach (AvailabilityPeriod period in periods)
            {
                if(filteredPeriods.Any(x => x.GuildId == period.GuildId && x.UserId == period.UserId))
                {
                    AvailabilityPeriod oldPeriod = filteredPeriods.First(x => x.GuildId == period.GuildId && x.UserId == period.UserId);
                    if (period.Expires > oldPeriod.Expires)
                    {
                        filteredPeriods.Remove(oldPeriod);
                        filteredPeriods.Add(period);
                    }
                }
                else
                {
                    filteredPeriods.Add(period);
                }
            }

            return filteredPeriods.OrderByDescending(x => x.Expires).ToList();
        }
    }
}
