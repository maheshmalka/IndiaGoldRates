using IndiaGoldRates.Core.Enums;

namespace IndiaGoldRates.Core.Entities;

/// <summary>
/// One row per (Metal, Purity) per poll cycle. Deliberately has no City column: the current
/// data source is a global spot price, so all cities are numerically identical until a real
/// per-city source is introduced. City stays a UI/notification-rule-only concept in v1.
/// </summary>
public class RateSnapshot
{
    public long Id { get; set; }

    public Metal Metal { get; set; }
    public Purity Purity { get; set; }
    public decimal PriceInrPerGram { get; set; }
    public decimal SpotPriceUsdPerOz { get; set; }
    public decimal UsdToInrRate { get; set; }
    public DateTime CapturedAtUtc { get; set; }
}
