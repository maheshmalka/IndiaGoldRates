namespace IndiaGoldRates.Core.Interfaces;

/// <summary>Fetches the USD-to-INR exchange rate from an external source.</summary>
public interface ICurrencyRateProvider
{
    Task<decimal> GetUsdToInrRateAsync(CancellationToken cancellationToken);
}
