using BlazorUI.Features;
using BlazorUI.Services;
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
builder.Services.AddJSComponents();
builder.Services.AddScoped<ICookieService, CookieService>();

if (bool.TryParse(builder.Configuration["ServiceBus:UseEmulator"], out bool useEmulator) && useEmulator)
{
    builder.Services.AddSingleton<IServiceBusClient>(sp =>
        new LocalServiceBusClient(builder.Configuration["ConnectionStrings:Local"]
            ?? throw new Exception("Missing 'ConnectionStrings:Local' in configuration")));
}
else
{
    builder.Services.AddSingleton<IServiceBusClient>(sp =>
        new AzureServiceBusClient(builder.Configuration["ConnectionStrings:Azure"]
            ?? throw new Exception("Missing 'ConnectionStrings:Azure' in configuration")));
}

builder.Services.AddTransient<IMessageService, MessageService>();
WebApplication app = builder.Build();

app.UseExceptionHandler("/Error", createScopeForErrors: true);

if (app.Environment.IsDevelopment() == false)
{
    app.UseHsts();
    app.UseResponseCompression();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
  .AddInteractiveServerRenderMode()
  .AllowAnonymous();
app.Run();
