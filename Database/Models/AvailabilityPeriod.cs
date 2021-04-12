using System;

namespace Database.Models
{
    public class AvailabilityPeriod
    {
        public int Id { get; set; }
        public ulong GuildId { get; set; }
        public ulong UserId { get; set; }
        public DateTime Starts { get; set; } = DateTime.MinValue;
        public DateTime Expires { get; set; }
    }
}
