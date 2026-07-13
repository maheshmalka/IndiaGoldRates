using IndiaGoldRates.Core.Enums;

namespace IndiaGoldRates.Core.Models;

public static class CurrentRatesViewExtensions
{
    public static decimal? GetPrice(this CurrentRatesView rates, Metal metal, Purity purity) =>
        (metal, purity) switch
        {
            (Metal.Gold, Purity.TwentyFourK) => rates.GoldTwentyFourK.PriceInrPerGram,
            (Metal.Gold, Purity.TwentyTwoK) => rates.GoldTwentyTwoK.PriceInrPerGram,
            (Metal.Silver, Purity.Pure) => rates.Silver.PriceInrPerGram,
            _ => null
        };
}
