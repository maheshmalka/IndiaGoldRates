using IndiaGoldRates.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IndiaGoldRates.Api.Controllers;

[ApiController]
[Route("api/rates")]
public class RatesController(IRateCache rateCache) : ControllerBase
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
}
