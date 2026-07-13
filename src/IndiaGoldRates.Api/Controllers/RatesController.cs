using IndiaGoldRates.Core.Enums;
using IndiaGoldRates.Core.Interfaces;
using IndiaGoldRates.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndiaGoldRates.Api.Controllers;

public record HistoryPoint(DateTime BucketStartUtc, decimal PriceInrPerGram);

[ApiController]
[Route("api/rates")]
public class RatesController(IRateCache rateCache, AppDbContext db) : ControllerBase
{
    /// <summary>Latest computed rates, served from the in-memory cache (no auth required — public feature).</summary>
    [HttpGet("current")]
    public IActionResult GetCurrent()
    {
        var current = rateCache.Current;
        if (current is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "Rates are not available yet — the first poll cycle hasn't completed."
            });
        }

        return Ok(current);
    }

    /// <summary>
    /// Rate history for one (metal, purity) series, bucketed server-side to keep the payload small
    /// for longer ranges: 1-minute buckets up to 24h, 15-minute buckets up to 7d, hourly beyond that.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Metal metal,
        [FromQuery] Purity purity,
        [FromQuery] int rangeHours = 24,
        CancellationToken cancellationToken = default)
    {
        if (rangeHours is <= 0 or > 24 * 30)
        {
            return BadRequest(new { message = "rangeHours must be between 1 and 720 (30 days)." });
        }

        var bucketMinutes = rangeHours switch
        {
            <= 24 => 1,
            <= 24 * 7 => 15,
            _ => 60
        };

        var cutoffUtc = DateTime.UtcNow.AddHours(-rangeHours);

        var rows = await db.RateSnapshots
            .Where(s => s.Metal == metal && s.Purity == purity && s.CapturedAtUtc >= cutoffUtc)
            .OrderBy(s => s.CapturedAtUtc)
            .Select(s => new { s.CapturedAtUtc, s.PriceInrPerGram })
            .ToListAsync(cancellationToken);

        var bucketed = rows
            .GroupBy(r => BucketStart(r.CapturedAtUtc, bucketMinutes))
            .Select(g => new HistoryPoint(g.Key, Math.Round(g.Average(r => r.PriceInrPerGram), 4)))
            .OrderBy(p => p.BucketStartUtc)
            .ToList();

        return Ok(bucketed);
    }

    private static DateTime BucketStart(DateTime capturedAtUtc, int bucketMinutes)
    {
        var ticksPerBucket = TimeSpan.FromMinutes(bucketMinutes).Ticks;
        var bucketedTicks = capturedAtUtc.Ticks / ticksPerBucket * ticksPerBucket;
        return new DateTime(bucketedTicks, DateTimeKind.Utc);
    }
}
