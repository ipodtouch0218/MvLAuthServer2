using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MvLAuthServer2;
using MvLAuthServer2.Endpoints;
using MvLAuthServer2.Services;
using MvLAuthServer2.Services.Impl;

// Create application
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddDbContext<MvLDBContext>(options => {
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection"));
    options.EnableDetailedErrors(true);
});
builder.Services.AddSingleton(_ => LogManager.GetLogger("logger"));

builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddSingleton<IConfigFileService, ConfigFileService>();

builder.Services.AddScoped<IBanService, BanService>();
builder.Services.AddScoped<IUserEntryLogService, UserEntryLogService>();

builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

// Redirect to the main page if we don't access an API endpoint
app.MapGet("/", () => Results.Redirect("https://ipodtouch0218.itch.io/nsmb-mariovsluigi"));
app.MapGet("/ping", () => Results.Ok("Pong! Hello modloader!"));

// Admin pages
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/admin") && !context.Request.Path.StartsWithSegments("/admin/login"))
    {
        // already logged in?
        if (!context.Request.Cookies.TryGetValue("AdminAuth", out var cookie) || cookie != "ok")
        {
            // not logged in, redirect to login page
            context.Response.Redirect("/admin/login");
            return;
        }
    }

    await next();
});
app.UseRouting();
app.MapRazorPages();

// API stuffs
app.MapNewsEndpoints();
app.MapAuthenticationEndpoints();
app.MapPunWebhookEndpoints();
app.MapQuantumWebhookEndpoints();
app.MapDiscordEndpoints();
app.MapDesyncHelperEndpoints();

// Done. Run
app.Run();