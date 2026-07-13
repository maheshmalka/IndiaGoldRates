namespace IndiaGoldRates.Infrastructure;

/// <summary>Bound from the "RateConversion" config section so purity factors can be tuned without a code change.</summary>
public class RateConversionOptions
{
    public const string SectionName = "RateConversion";

    /// <summary>Fraction of pure gold value that 24K gold trades at (e.g. 0.999 for 99.9% purity).</summary>
    public decimal Gold24KPurityFactor { get; set; } = 0.999m;

    /// <summary>Ratio of 22K gold value relative to 24K gold value (standard is 22/24).</summary>
    public decimal Gold22KToGold24KRatio { get; set; } = 22.0m / 24.0m;

    /// <summary>Troy ounces to grams conversion constant.</summary>
    public decimal GramsPerTroyOunce { get; set; } = 31.1034768m;
}
