using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using LuseGateway.Core.Data;
using LuseGateway.Core.Services;
using LuseGateway.Fix;
using LuseGateway.Service;
using LuseGateway.Service.Hubs;
using LuseGateway.Service.Services;
using LuseGateway.Fix.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Web and Blazor Infrastructure
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSignalR();

// 2. Database Configuration
builder.Services.AddDbContext<LuseDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LuseDatabase"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// 3. Core Services
builder.Services.AddScoped<IOrderService, OrderService>();

// 4. FIX Layer
builder.Services.AddSingleton<LuseMessageCracker>();
builder.Services.AddSingleton<LuseFixApplication>();

// 5. Dashboard Services
builder.Services.AddSingleton<IFixMessageLogService, FixMessageLogService>();
builder.Services.AddScoped<IFixActionService, FixActionService>();

// 6. Background Worker
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapHub<GatewayHub>("/gatewayHub");
app.MapFallbackToPage("/_Host");

app.Run();
