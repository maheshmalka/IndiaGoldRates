using IndiaGoldRates.Core.Entities;
using IndiaGoldRates.Core.Enums;
using IndiaGoldRates.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace IndiaGoldRates.Infrastructure;

public class RateConversionService(IOptions<RateConversionOptions> options) : IRateConversionService
{
    private readonly RateConversionOptions _options = options.Value;

    public IReadOnlyList<RateSnapshot> Convert(
        decimal goldSpotUsdPerOz,
        decimal silverSpotUsdPerOz,
        decimal usdToInrRate,
        DateTime capturedAtUtc)
    {
        var goldInrPerGramPure = goldSpotUsdPerOz * usdToInrRate / _options.GramsPerTroyOunce;
        var silverInrPerGram = silverSpotUsdPerOz * usdToInrRate / _options.GramsPerTroyOunce;

        var gold24K = goldInrPerGramPure * _options.Gold24KPurityFactor;
        var gold22K = gold24K * _options.Gold22KToGold24KRatio;

        return
        [
            new RateSnapshot
            {
                Metal = Metal.Gold,
                Purity = Purity.TwentyFourK,
                PriceInrPerGram = gold24K,
                SpotPriceUsdPerOz = goldSpotUsdPerOz,
                UsdToInrRate = usdToInrRate,
                CapturedAtUtc = capturedAtUtc
            },
            new RateSnapshot
            {
                Metal = Metal.Gold,
                Purity = Purity.TwentyTwoK,
                PriceInrPerGram = gold22K,
                SpotPriceUsdPerOz = goldSpotUsdPerOz,
                UsdToInrRate = usdToInrRate,
                CapturedAtUtc = capturedAtUtc
            },
            new RateSnapshot
            {
                Metal = Metal.Silver,
                Purity = Purity.Pure,
                PriceInrPerGram = silverInrPerGram,
                SpotPriceUsdPerOz = silverSpotUsdPerOz,
                UsdToInrRate = usdToInrRate,
                CapturedAtUtc = capturedAtUtc
            }
        ];
    }
}
