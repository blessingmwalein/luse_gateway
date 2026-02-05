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
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Database Configuration
builder.Services.AddPooledDbContextFactory<LuseDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LuseDatabase"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// Register scoped DbContext for services that inject LuseDbContext directly
builder.Services.AddScoped<LuseDbContext>(provider =>
{
    var factory = provider.GetRequiredService<IDbContextFactory<LuseDbContext>>();
    return factory.CreateDbContext();
});

// 3. Core Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderReconciliationService, OrderReconciliationService>();
builder.Services.AddSingleton<IPartyService, PartyService>();
builder.Services.AddScoped<IBillingService, BillingService>();

// 3.1 STT Web API Client
builder.Services.AddHttpClient<ISttApiClient, SttApiClient>();
builder.Services.AddScoped<ISttApiClient, SttApiClient>();

// 4. FIX Layer
builder.Services.AddSingleton<LuseMessageCracker>();
builder.Services.AddSingleton<LuseFixApplication>();

// 5. Dashboard Services
builder.Services.AddSingleton<IFixMessageLogService, FixMessageLogService>();
builder.Services.AddScoped<IFixActionService, FixActionService>();

// 6. Background Worker
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapBlazorHub();
app.MapHub<GatewayHub>("/gatewayHub");
app.MapFallbackToPage("/_Host");

app.Run();
