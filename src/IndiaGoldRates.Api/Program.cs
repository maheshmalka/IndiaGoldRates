using IndiaGoldRates.Api.Hubs;
using IndiaGoldRates.Api.Workers;
using IndiaGoldRates.Core.Entities;
using IndiaGoldRates.Core.Interfaces;
using IndiaGoldRates.Infrastructure;
using IndiaGoldRates.Infrastructure.Data;
using IndiaGoldRates.Infrastructure.ExternalApis;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

const string DevCorsPolicy = "DevCorsPolicy";

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
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

// External-login-only Identity: no local password store, ApplicationUser rows are only ever
// created via the Google/Microsoft callback flow in AuthController.
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    // SPA (Static Web Apps) and API (App Service) are different origins in production, so the
    // auth cookie must be sendable cross-site. SameSite=None requires Secure, hence the https
    // launch profile for local dev (see README) — plain http won't carry this cookie in Chrome.
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// ASP.NET Core's OAuth handlers validate their options (including ClientId) on every request —
// not just login attempts — because RemoteAuthenticationHandler checks each registered scheme's
// CallbackPath against the current request. An empty ClientId would therefore 500 every request,
// not just OAuth ones. So each provider is only registered once real credentials are configured
// (e.g. via `dotnet user-secrets set "Authentication:Google:ClientId" ...`), letting the rest of
// the app run normally before OAuth apps have been created.
var authenticationBuilder = builder.Services.AddAuthentication();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
if (!string.IsNullOrEmpty(googleClientId))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });
}

var microsoftClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
if (!string.IsNullOrEmpty(microsoftClientId))
{
    authenticationBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = microsoftClientId;
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
        options.CallbackPath = "/signin-microsoft";
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });
}

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RatesHub>("/hubs/rates");

app.Run();
