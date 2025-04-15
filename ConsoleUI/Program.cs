using FileLoggerLibrary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceBusLibrary.Interfaces;
using ServiceBusLibrary.Services;

namespace ConsoleUI;

internal class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            bool isDevelopment = string.IsNullOrEmpty(env) 
                || env.Equals("development", StringComparison.CurrentCultureIgnoreCase);

            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appSettings.json", true, true);
                    config.AddJsonFile($"appSettings.{env}.json", true, true);
                    config.AddUserSecrets<Program>(optional: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders();
                    builder.AddFileLogger(context.Configuration);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    if (bool.TryParse(hostContext.Configuration["ServiceBus:UseEmulator"], out bool useEmulator) && useEmulator)
                    {
                        services.AddSingleton<IServiceBusClient>(sp =>
                            new LocalServiceBusClient(
                                hostContext.Configuration["ConnectionStrings:Local"]
                                ?? throw new Exception("Missing ConnectionStrings:Local in app configuration")));
                    }
                    else
                    {
                        services.AddSingleton<IServiceBusClient>(sp =>
                            new AzureServiceBusClient(
                                hostContext.Configuration["ConnectionStrings:Azure"]
                                ?? throw new Exception("Missing ConnectionStrings:Azure in app configuration")));
                    }
                    services.AddHostedService<App>();
                })
                .RunConsoleAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}
