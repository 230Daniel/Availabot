using System.IO;
using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Database.Contexts
{
    public class DatabaseContext : DbContext
    {
        public DbSet<GuildConfiguration> GuildConfigurations { get; set; }
        public DbSet<AvailabilityPeriod> AvailabilityPeriods { get; set; }

        readonly string _connectionString;

        public DatabaseContext(IConfiguration configuration)
        {
            DatabaseConfiguration dbConfiguration = new DatabaseConfiguration();
            configuration.GetSection("database")
                .Bind(dbConfiguration);

            _connectionString = 
                $"Server={dbConfiguration.Server};" +
                $"Port={dbConfiguration.Port};" +
                $"Database={dbConfiguration.Database};" +
                $"User Id={dbConfiguration.UserId};" +
                $"Password={dbConfiguration.Password};";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseNpgsql(_connectionString);
    }

    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DatabaseContext> 
    { 
        public DatabaseContext CreateDbContext(string[] args) 
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@Directory.GetCurrentDirectory() + "/../Availabot/appsettings.json")
                .Build();

            return new DatabaseContext(configuration);
        } 
    }
}
