using Syncfusion.Blazor;
using TcneCalendar;
using TcneCalendar.Components;
using Microsoft.Extensions.Azure;
using System.Diagnostics;
using Microsoft.Extensions.Logging.AzureAppServices;
using TcneShared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSyncfusionBlazor();

var configuration = builder.Configuration;

//builder.Services.AddHttpClient("MyHttpClient", client =>
//{
//    string? uriCheckFront = configuration["CheckFront_Api_Url"];
//    if (uriCheckFront != null)
//    {
//        client.BaseAddress = new Uri(uriCheckFront);
//    }
//    // Additional configuration options for the HttpClient can be set here
//});

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<CheckFrontApiService>();
builder.Services.AddSingleton<AzureStorage>();

builder.Services.AddLogging();
builder.Services.AddHttpClient();
//builder.Services.AddMemoryCache();

// Azure App Service logging
builder.Logging.AddAzureWebAppDiagnostics();

builder.Services.Configure <AzureFileLoggerOptions>(options =>
{
    options.FileName = "log.txt";
    options.FileSizeLimit = 50 * 1024;
    options.RetainedFileCountLimit = 5;
});

builder.Services.Configure<AzureBlobLoggerOptions>(options =>
{
    options.BlobName = "log.txt";
});  



builder.Services.AddAzureClients(clientBuilder =>
{
    string? cs = configuration["StorageConnectionString:blob"];
    if (!String.IsNullOrEmpty(cs))
    {
        clientBuilder.AddBlobServiceClient(cs, preferMsi: true);
    }
});






var app = builder.Build();

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(configuration["SyncFusionLicenseKey"]);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Custom middleware to add the X-Frame-Options header
app.Use(async (context, next) =>
{
    string? allowUri = configuration["X-Frame-Options ALLOW-FROM"];
    if (allowUri != null)
    {
        context.Response.Headers.Append($"X-Frame-Options", "ALLOW-FROM {allowUri}");
    }
    await next();
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
