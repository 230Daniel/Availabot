using System;
using Availabot.Commands.TypeParsers;
using Disqord;
using Disqord.Bot.Hosting;
using Disqord.Extensions.Interactivity;
using Disqord.Gateway;
using Availabot.Implementations;
using Availabot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Database.Contexts;
using Disqord.Bot;

namespace Availabot
{
    class Program
    {
        static void Main()
        {
            IHost host = Host.CreateDefaultBuilder()
                .ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddProvider(new LoggerProvider());
                })
                .ConfigureServices(ConfigureServices)
                .ConfigureDiscordBotSharder<MyDiscordBotSharder>((context, bot) =>
                {
                    bot.Token = context.Configuration["token"];
                    bot.ReadyEventDelayMode = ReadyEventDelayMode.Guilds;
                    bot.Intents += GatewayIntent.Members;
                    bot.Intents += GatewayIntent.VoiceStates;
                    bot.Activities = new[] { new LocalActivity($"{context.Configuration["prefix"]}help", ActivityType.Playing)};
                    bot.OwnerIds = new[] { new Snowflake(218613903653863427) };
                    bot.ShardCount = 1;
                })
                .Build();

            try
            {
                using (host)
                {
                    host.Run();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
            }
        }

        static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddInteractivity();
            services.AddPrefixProvider<PrefixProvider>();
            services.AddDbContextFactory<DatabaseContext>();
            services.AddSingleton<AvailabilityService>();
            services.AddSingleton<TimeSpanTypeParser>();
        }
    }
}
