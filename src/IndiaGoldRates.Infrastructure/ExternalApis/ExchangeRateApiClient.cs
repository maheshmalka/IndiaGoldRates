using System.Net.Http.Json;
using IndiaGoldRates.Core.Interfaces;

namespace IndiaGoldRates.Infrastructure.ExternalApis;

/// <summary>Calls the free, unauthenticated open.er-api.com exchange rate endpoint for USD-to-INR.</summary>
public class ExchangeRateApiClient(HttpClient httpClient) : ICurrencyRateProvider
{
    private record ExchangeRateResponse(string Result, Dictionary<string, decimal> Rates);

    public async Task<decimal> GetUsdToInrRateAsync(CancellationToken cancellationToken)
    {
        var response = await httpClient.GetFromJsonAsync<ExchangeRateResponse>(
            "v6/latest/USD", cancellationToken)
            ?? throw new InvalidOperationException("open.er-api.com returned an empty response.");

        if (response.Result != "success" || !response.Rates.TryGetValue("INR", out var rate))
        {
            throw new InvalidOperationException("open.er-api.com response did not contain a usable INR rate.");
        }

        return rate;
    }
}
