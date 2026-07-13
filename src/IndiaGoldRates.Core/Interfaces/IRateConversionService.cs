using IndiaGoldRates.Core.Entities;

namespace IndiaGoldRates.Core.Interfaces;

/// <summary>Converts raw USD/troy-oz spot prices + an FX rate into INR-per-gram values for each tracked (Metal, Purity).</summary>
public interface IRateConversionService
{
    IReadOnlyList<RateSnapshot> Convert(
        decimal goldSpotUsdPerOz,
        decimal silverSpotUsdPerOz,
        decimal usdToInrRate,
        DateTime capturedAtUtc);
}
