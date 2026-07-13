using IndiaGoldRates.Core.Enums;
using IndiaGoldRates.Infrastructure;
using Microsoft.Extensions.Options;

namespace IndiaGoldRates.Api.Tests;

public class RateConversionServiceTests
{
    private static RateConversionService CreateService() =>
        new(Options.Create(new RateConversionOptions()));

    [Fact]
    public void Convert_ProducesThreeSnapshots_ForGold24K_Gold22K_AndSilver()
    {
        var service = CreateService();

        var snapshots = service.Convert(
            goldSpotUsdPerOz: 4000m,
            silverSpotUsdPerOz: 50m,
            usdToInrRate: 87m,
            capturedAtUtc: DateTime.UtcNow);

        Assert.Equal(3, snapshots.Count);
        Assert.Contains(snapshots, s => s.Metal == Metal.Gold && s.Purity == Purity.TwentyFourK);
        Assert.Contains(snapshots, s => s.Metal == Metal.Gold && s.Purity == Purity.TwentyTwoK);
        Assert.Contains(snapshots, s => s.Metal == Metal.Silver && s.Purity == Purity.Pure);
    }

    [Fact]
    public void Convert_Gold22K_IsLowerThan24K_ByThePurityRatio()
    {
        var service = CreateService();

        var snapshots = service.Convert(4000m, 50m, 87m, DateTime.UtcNow);

        var gold24K = snapshots.Single(s => s.Purity == Purity.TwentyFourK).PriceInrPerGram;
        var gold22K = snapshots.Single(s => s.Purity == Purity.TwentyTwoK).PriceInrPerGram;

        var expectedRatio = 22.0m / 24.0m;
        var actualRatio = gold22K / gold24K;

        Assert.True(Math.Abs(actualRatio - expectedRatio) < 0.0001m,
            $"Expected 22K/24K ratio ~{expectedRatio}, got {actualRatio}");
    }

    [Fact]
    public void Convert_HigherFxRate_ProducesHigherInrPrices()
    {
        var service = CreateService();

        var lowFx = service.Convert(4000m, 50m, 80m, DateTime.UtcNow);
        var highFx = service.Convert(4000m, 50m, 90m, DateTime.UtcNow);

        var lowFx24K = lowFx.Single(s => s.Purity == Purity.TwentyFourK).PriceInrPerGram;
        var highFx24K = highFx.Single(s => s.Purity == Purity.TwentyFourK).PriceInrPerGram;

        Assert.True(highFx24K > lowFx24K);
    }

    [Fact]
    public void Convert_KnownInputs_ProducesExpectedInrPerGram()
    {
        var service = CreateService();

        // 4000 USD/oz * 87 INR/USD / 31.1034768 g/oz = 11,192.06... INR/g (pure), * 0.999 purity factor for 24K
        var snapshots = service.Convert(4000m, 50m, 87m, DateTime.UtcNow);

        var gold24K = snapshots.Single(s => s.Purity == Purity.TwentyFourK).PriceInrPerGram;
        var expected = 4000m * 87m / 31.1034768m * 0.999m;

        Assert.True(Math.Abs(gold24K - expected) < 0.01m);
    }
}
