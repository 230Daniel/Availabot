using System.ComponentModel.DataAnnotations;

namespace Database.Models
{
    public class GuildConfiguration
    {
        [Key]
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong RoleId { get; set; }

        public GuildConfiguration(ulong guildId)
        {
            GuildId = guildId;
        }

        public GuildConfiguration() { }
    }
}
