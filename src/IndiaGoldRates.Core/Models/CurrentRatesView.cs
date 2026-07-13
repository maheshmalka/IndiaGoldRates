namespace IndiaGoldRates.Core.Models;

public record MetalRateView(decimal PriceInrPerGram);

/// <summary>Read model for the public "current rates" REST endpoint and the SignalR broadcast payload.</summary>
public record CurrentRatesView(
    MetalRateView GoldTwentyTwoK,
    MetalRateView GoldTwentyFourK,
    MetalRateView Silver,
    bool IsStale,
    DateTime SourceUpdatedAtUtc);
