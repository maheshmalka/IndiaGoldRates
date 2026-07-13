using IndiaGoldRates.Core.Enums;

namespace IndiaGoldRates.Core.Interfaces;

public record SpotPriceQuote(Metal Metal, decimal PriceUsdPerOz, DateTime SourceUpdatedAtUtc);

/// <summary>Fetches the raw USD-per-troy-ounce spot price for a metal from an external source.</summary>
public interface IPreciousMetalRateProvider
{
    Task<SpotPriceQuote> GetSpotPriceAsync(Metal metal, CancellationToken cancellationToken);
}
