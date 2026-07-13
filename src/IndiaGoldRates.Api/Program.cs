using IndiaGoldRates.Api.Hubs;
using IndiaGoldRates.Api.Workers;
using IndiaGoldRates.Core.Interfaces;
using IndiaGoldRates.Infrastructure;
using IndiaGoldRates.Infrastructure.Data;
using IndiaGoldRates.Infrastructure.ExternalApis;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

const string DevCorsPolicy = "DevCorsPolicy";

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.Configure<RateConversionOptions>(
    builder.Configuration.GetSection(RateConversionOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite"));
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    }
});

static IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) +
            TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)));

builder.Services.AddHttpClient<IPreciousMetalRateProvider, GoldApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["GoldApi:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
}).AddPolicyHandler(BuildRetryPolicy());

builder.Services.AddHttpClient<ICurrencyRateProvider, ExchangeRateApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExchangeRateApi:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(10);
}).AddPolicyHandler(BuildRetryPolicy());

builder.Services.AddSingleton<IRateCache, RateCache>();
builder.Services.AddSingleton<IRateConversionService, RateConversionService>();
builder.Services.AddHostedService<RatePollingBackgroundService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy => policy
        .WithOrigins(builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(DevCorsPolicy);
app.UseAuthorization();

app.MapControllers();
app.MapHub<RatesHub>("/hubs/rates");

app.Run();
