using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
