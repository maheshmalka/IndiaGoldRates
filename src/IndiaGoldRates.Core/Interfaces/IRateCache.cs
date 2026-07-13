using IndiaGoldRates.Core.Models;

namespace IndiaGoldRates.Core.Interfaces;

/// <summary>In-memory holder of the latest computed rates, for fast REST reads without hitting the DB.</summary>
public interface IRateCache
{
    CurrentRatesView? Current { get; }
    void Set(CurrentRatesView rates);
    void MarkStale();
}
