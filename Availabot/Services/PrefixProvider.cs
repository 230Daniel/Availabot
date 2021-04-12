using System.Collections.Generic;
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

        public ValueTask<IEnumerable<IPrefix>> GetPrefixesAsync(IGatewayUserMessage message)
        {
            return ValueTask.FromResult<IEnumerable<IPrefix>>(new IPrefix[]
            {
                new StringPrefix(_config.GetValue<string>("prefix")),
                new MentionPrefix(_config.GetValue<ulong>("userId"))
            });
        }
    }
}
