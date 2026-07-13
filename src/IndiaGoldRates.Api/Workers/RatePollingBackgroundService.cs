using IndiaGoldRates.Api.Hubs;
using IndiaGoldRates.Core.Enums;
using IndiaGoldRates.Core.Interfaces;
using IndiaGoldRates.Core.Models;
using IndiaGoldRates.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;

namespace IndiaGoldRates.Api.Workers;

/// <summary>
/// Polls the external spot-price API every 60s and the FX-rate API every 60min, computes INR/gram
/// for Gold 24K/22K and Silver, updates the in-memory cache, persists a RateSnapshot per metal/purity,
/// broadcasts to all SignalR clients, and runs notification-rule evaluation each successful cycle.
/// </summary>
public class RatePollingBackgroundService(
    IPreciousMetalRateProvider metalRateProvider,
    ICurrencyRateProvider currencyRateProvider,
    IRateConversionService conversionService,
    IRateCache rateCache,
    IHubContext<RatesHub> hubContext,
    IServiceScopeFactory scopeFactory,
    ILogger<RatePollingBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SpotPricePollInterval = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan FxRatePollInterval = TimeSpan.FromMinutes(60);

    private readonly SemaphoreSlim _cycleGuard = new(1, 1);
    private decimal? _cachedUsdToInrRate;
    private DateTime _fxRateFetchedAtUtc = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SpotPricePollInterval);

        // Run one cycle immediately on startup instead of waiting for the first tick.
        await RunCycleAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleAsync(stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        if (!await _cycleGuard.WaitAsync(0, cancellationToken))
        {
            logger.LogWarning("Skipping poll cycle — previous cycle is still running.");
            return;
        }

        try
        {
            var usdToInrRate = await GetUsdToInrRateAsync(cancellationToken);
            if (usdToInrRate is null)
            {
                MarkCacheStale("USD-to-INR rate unavailable.");
                return;
            }

            var goldQuote = await TryGetSpotPriceAsync(Metal.Gold, cancellationToken);
            var silverQuote = await TryGetSpotPriceAsync(Metal.Silver, cancellationToken);

            if (goldQuote is null || silverQuote is null)
            {
                MarkCacheStale("Spot price fetch failed.");
                return;
            }

            var capturedAtUtc = DateTime.UtcNow;
            var snapshots = conversionService.Convert(
                goldQuote.PriceUsdPerOz, silverQuote.PriceUsdPerOz, usdToInrRate.Value, capturedAtUtc);

            var sourceUpdatedAtUtc = goldQuote.SourceUpdatedAtUtc > silverQuote.SourceUpdatedAtUtc
                ? goldQuote.SourceUpdatedAtUtc
                : silverQuote.SourceUpdatedAtUtc;

            var view = new CurrentRatesView(
                GoldTwentyTwoK: new MetalRateView(snapshots.Single(s => s.Metal == Metal.Gold && s.Purity == Purity.TwentyTwoK).PriceInrPerGram),
                GoldTwentyFourK: new MetalRateView(snapshots.Single(s => s.Metal == Metal.Gold && s.Purity == Purity.TwentyFourK).PriceInrPerGram),
                Silver: new MetalRateView(snapshots.Single(s => s.Metal == Metal.Silver).PriceInrPerGram),
                IsStale: false,
                SourceUpdatedAtUtc: sourceUpdatedAtUtc);

            rateCache.Set(view);

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.RateSnapshots.AddRange(snapshots);
                await db.SaveChangesAsync(cancellationToken);

                var evaluator = scope.ServiceProvider.GetRequiredService<INotificationRuleEvaluator>();
                await evaluator.EvaluateAsync(view, capturedAtUtc, cancellationToken);
            }

            await hubContext.Clients.All.SendAsync("RatesUpdated", view, cancellationToken);
            logger.LogInformation(
                "Rates updated: 24K={Gold24K} 22K={Gold22K} Silver={Silver} INR/g",
                view.GoldTwentyFourK.PriceInrPerGram, view.GoldTwentyTwoK.PriceInrPerGram, view.Silver.PriceInrPerGram);
        }
        finally
        {
            _cycleGuard.Release();
        }
    }

    private async Task<decimal?> GetUsdToInrRateAsync(CancellationToken cancellationToken)
    {
        if (_cachedUsdToInrRate.HasValue && DateTime.UtcNow - _fxRateFetchedAtUtc < FxRatePollInterval)
        {
            return _cachedUsdToInrRate;
        }

        try
        {
            var rate = await currencyRateProvider.GetUsdToInrRateAsync(cancellationToken);
            _cachedUsdToInrRate = rate;
            _fxRateFetchedAtUtc = DateTime.UtcNow;
            return rate;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch USD-to-INR rate.");
            return _cachedUsdToInrRate; // fall back to last-known-good, may be null on first-ever failure
        }
    }

    private async Task<SpotPriceQuote?> TryGetSpotPriceAsync(Metal metal, CancellationToken cancellationToken)
    {
        try
        {
            return await metalRateProvider.GetSpotPriceAsync(metal, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch {Metal} spot price.", metal);
            return null;
        }
    }

    private void MarkCacheStale(string reason)
    {
        logger.LogWarning("Marking rate cache stale: {Reason}", reason);
        rateCache.MarkStale();
    }
}
