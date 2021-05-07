using System;
using System.Collections.Generic;
using System.Text;
using Azure.Identity;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Skandisoft.KeyInsight.Sources.Mail;

[assembly: FunctionsStartup(typeof(Skandisoft.KeyInsight.Sources.Startup))]
namespace Skandisoft.KeyInsight.Sources
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            var config = builder.ConfigurationBuilder;
            var curSettings = config.Build();

            // if you are experiencing issues authenticating, 
            // try relogging visual studio  
            // that should solve the problem
            var creds = new AzureCliCredential();

            config.AddAzureKeyVault(new Uri(curSettings["KeyVaultUrl"]), creds);
            config.AddAzureAppConfiguration(curSettings["AppConfigConnectionString"]);
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var collection = builder.Services;
            var config = builder.GetContext().Configuration;

            AddLogging(collection);
            AddCosmosDb(collection, config);

            collection.AddOptions<EmailConfiguration>()
                .Configure<IConfiguration>((opt, conf)
                    => conf.GetSection("Email").Bind(opt));

            collection.AddSingleton<IMailClient, MailClient>();
        }

        private IServiceCollection AddCosmosDb(IServiceCollection collection, IConfiguration config)
        {
            var builder = new CosmosClientBuilder(config["Cosmos:ConnectionString"])
                .WithSerializerOptions(new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                    IgnoreNullValues = true
                });

            return collection.AddSingleton(builder.Build());
        }

        private IServiceCollection AddLogging(IServiceCollection collection)
        {
            var log = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Seq("http://localhost:5341/")
                .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Events)
                .WriteTo.Console()
                .CreateLogger();

            collection.AddLogging(x => x.AddSerilog(log));

            return collection;
        }
    }
}
