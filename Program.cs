using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Poker.Hubs;
using Poker.Pages;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(); // ✅ Correct for .NET 8

builder.Services.AddSignalR();  // ✅ Add SignalR service
builder.Services.AddAntiforgery();  // ✅ Add Anti-forgery service

var app = builder.Build();

// Setup environment and error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAntiforgery(); // ✅ Add this line BEFORE Blazor routing

// ✅ Ensure Blazor routing works properly
app.MapHub<GameHub>("/gamehub"); // ✅ Map SignalR hub for chat
app.MapRazorComponents<Poker.Pages.App>() 
    .AddInteractiveServerRenderMode();

app.Run();
