using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Disqord.Bot;
using Disqord.Gateway;
using Microsoft.Extensions.Configuration;

namespace Availabot.Services
{
    class PrefixProvider : IPrefixProvider
    {
        IConfiguration _config;

        public PrefixProvider(IConfiguration config)
        {
            _config = config;
        }

        public async ValueTask<IEnumerable<IPrefix>> GetPrefixesAsync(IGatewayUserMessage message)
        {
            return new IPrefix[]
            {
                new StringPrefix(_config.GetValue<string>("prefix")),
                new MentionPrefix(_config.GetValue<ulong>("userId"))
            };
        }
    }
}
