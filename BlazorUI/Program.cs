using BlazorUI.Features;
using BlazorUI.Services;
using Microsoft.Extensions.Azure;
using ServiceBusLibrary.Interfaces;
using ServiceBusLibrary.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    });
builder.Services.AddResponseCompression();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICookieService, CookieService>();
builder.Services.AddAzureClients(options =>
{
    options.AddServiceBusClient(builder.Configuration.GetConnectionString("AzureServiceBus"));
});
builder.Services.AddTransient<IQueueService, QueueService>();
WebApplication app = builder.Build();

app.UseExceptionHandler("/Error", createScopeForErrors: true);

if (app.Environment.IsDevelopment() == false)
{
    app.UseHsts();
    app.UseResponseCompression();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();
app.Run();
