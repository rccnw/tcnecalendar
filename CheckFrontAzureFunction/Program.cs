using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TcneShared;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureLogging((context, loggingBuilder) =>
    {
        // Configure logging here
        loggingBuilder.AddApplicationInsights();

        // Add other logging providers as needed
        // For example, for console logging:
        loggingBuilder.AddConsole();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient();
        services.AddSingleton<CheckFrontApiService>();
        services.AddSingleton<AzureStorage>();
        services.AddSingleton(context.Configuration);
    })
    .ConfigureAppConfiguration((context, configBuilder) =>
    {
        configBuilder.SetBasePath(Directory.GetCurrentDirectory());
        configBuilder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        configBuilder.AddUserSecrets<Program>();
        configBuilder.AddEnvironmentVariables();
    })
    .Build();

host.Run();
