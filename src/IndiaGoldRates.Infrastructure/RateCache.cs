using IndiaGoldRates.Core.Interfaces;
using IndiaGoldRates.Core.Models;

namespace IndiaGoldRates.Infrastructure;

/// <summary>Thread-safe in-memory holder of the latest computed rates, registered as a singleton.</summary>
public class RateCache : IRateCache
{
    private CurrentRatesView? _current;
    private readonly Lock _lock = new();

    public CurrentRatesView? Current
    {
        get
        {
            lock (_lock)
            {
                return _current;
            }
        }
    }

    public void Set(CurrentRatesView rates)
    {
        lock (_lock)
        {
            _current = rates;
        }
    }

    public void MarkStale()
    {
        lock (_lock)
        {
            if (_current is not null)
            {
                _current = _current with { IsStale = true };
            }
        }
    }
}
