using Blazored.LocalStorage;
using ServiceBusLibrary.Interfaces;
using ServiceBusLibrary.Services;
using Microsoft.Extensions.Azure;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAzureClients(options =>
{
    options.AddServiceBusClient(builder.Configuration.GetConnectionString("ServiceBus"));
});
builder.Services.AddTransient<IQueueService, QueueService>();

var app = builder.Build();

if (app.Environment.IsDevelopment() == false)
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
