using System.Threading.Tasks;
using Disqord;
using Disqord.Rest;
using Availabot.Utils;

namespace Availabot.Extensions
{
    static class TextChannelExtensions
    {
        public static async Task<IUserMessage> SendInfoAsync(this ITextChannel channel, string title, string content = null, string raw = null)
        {
            LocalMessageBuilder builder = new LocalMessageBuilder()
                .WithEmbed(MessageUtils.CreateEmbed(EmbedType.Info, title, content));

            if(raw is not null) builder.Content = raw;

            return await channel.SendMessageAsync(builder.Build());
        }

        public static async Task<IUserMessage> SendSuccessAsync(this ITextChannel channel, string title, string content = null, string raw = null)
        {
            LocalMessageBuilder builder = new LocalMessageBuilder()
                .WithEmbed(MessageUtils.CreateEmbed(EmbedType.Success, title, content));

            if(raw is not null) builder.Content = raw;

            return await channel.SendMessageAsync(builder.Build());
        }

        public static async Task<IUserMessage> SendFailureAsync(this ITextChannel channel, string title, string content = null, bool supportLink = true, string raw = null)
        {
            LocalMessageBuilder builder = new LocalMessageBuilder()
                .WithEmbed(MessageUtils.CreateEmbed(EmbedType.Failure, title, content));

            if(raw is not null) builder.Content = raw;

            return await channel.SendMessageAsync(builder.Build());
        }

        public static async Task<IUserMessage> SendEmbedAsync(this ITextChannel channel, LocalEmbedBuilder embed)
        {
            LocalMessage message = new LocalMessageBuilder()
                .WithEmbed(embed)
                .Build();

            return await channel.SendMessageAsync(message);
        }
    }
}
