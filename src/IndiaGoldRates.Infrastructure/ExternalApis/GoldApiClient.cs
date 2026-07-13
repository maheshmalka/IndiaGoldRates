using System.Net.Http.Json;
using IndiaGoldRates.Core.Enums;
using IndiaGoldRates.Core.Interfaces;

namespace IndiaGoldRates.Infrastructure.ExternalApis;

/// <summary>Calls the free, unauthenticated gold-api.com spot price endpoint (api.gold-api.com/price/{symbol}).</summary>
public class GoldApiClient(HttpClient httpClient) : IPreciousMetalRateProvider
{
    private record GoldApiPriceResponse(string Symbol, decimal Price, DateTime UpdatedAt);

    public async Task<SpotPriceQuote> GetSpotPriceAsync(Metal metal, CancellationToken cancellationToken)
    {
        var symbol = metal switch
        {
            Metal.Gold => "XAU",
            Metal.Silver => "XAG",
            _ => throw new ArgumentOutOfRangeException(nameof(metal), metal, null)
        };

        var response = await httpClient.GetFromJsonAsync<GoldApiPriceResponse>(
            $"price/{symbol}", cancellationToken)
            ?? throw new InvalidOperationException($"gold-api.com returned an empty response for {symbol}.");

        return new SpotPriceQuote(metal, response.Price, response.UpdatedAt);
    }
}
