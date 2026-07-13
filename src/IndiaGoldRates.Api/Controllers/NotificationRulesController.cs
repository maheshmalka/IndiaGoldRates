using System.Security.Claims;
using IndiaGoldRates.Api.Contracts;
using IndiaGoldRates.Core.Entities;
using IndiaGoldRates.Core.Enums;
using IndiaGoldRates.Core.Interfaces;
using IndiaGoldRates.Core.Models;
using IndiaGoldRates.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndiaGoldRates.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notification-rules")]
public class NotificationRulesController(AppDbContext db, IRateCache rateCache) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var rules = await db.NotificationRules
            .Where(r => r.UserId == CurrentUserId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => ToDto(r))
            .ToListAsync(cancellationToken);

        return Ok(rules);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var rule = await db.NotificationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == CurrentUserId, cancellationToken);

        return rule is null ? NotFound() : Ok(ToDto(rule));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UpsertNotificationRuleRequest request, CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var validationError))
        {
            return BadRequest(new { message = validationError });
        }

        var now = DateTime.UtcNow;
        var rule = new NotificationRule
        {
            Id = Guid.NewGuid(),
            UserId = CurrentUserId,
            City = request.City,
            Metal = request.Metal,
            Purity = request.Purity,
            IsActive = request.IsActive,
            DigestEnabled = request.DigestEnabled,
            DigestFrequencyType = request.DigestFrequencyType,
            DigestTimeOfDay = request.DigestTimeOfDay,
            DigestIntervalHours = request.DigestIntervalHours,
            ThresholdEnabled = request.ThresholdEnabled,
            ThresholdAbsoluteRupees = request.ThresholdAbsoluteRupees,
            ThresholdPercent = request.ThresholdPercent,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        if (request.ThresholdEnabled)
        {
            var referencePrice = GetCurrentPrice(request.Metal, request.Purity);
            rule.ThresholdReferencePriceInrPerGram = referencePrice;
            rule.ThresholdReferenceSetAtUtc = referencePrice.HasValue ? now : null;
        }

        db.NotificationRules.Add(rule);
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, ToDto(rule));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpsertNotificationRuleRequest request, CancellationToken cancellationToken)
    {
        if (!TryValidate(request, out var validationError))
        {
            return BadRequest(new { message = validationError });
        }

        var rule = await db.NotificationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == CurrentUserId, cancellationToken);
        if (rule is null)
        {
            return NotFound();
        }

        var metalOrPurityChanged = rule.Metal != request.Metal || rule.Purity != request.Purity;
        var thresholdNewlyEnabled = request.ThresholdEnabled && !rule.ThresholdEnabled;

        rule.City = request.City;
        rule.Metal = request.Metal;
        rule.Purity = request.Purity;
        rule.IsActive = request.IsActive;
        rule.DigestEnabled = request.DigestEnabled;
        rule.DigestFrequencyType = request.DigestFrequencyType;
        rule.DigestTimeOfDay = request.DigestTimeOfDay;
        rule.DigestIntervalHours = request.DigestIntervalHours;
        rule.ThresholdEnabled = request.ThresholdEnabled;
        rule.ThresholdAbsoluteRupees = request.ThresholdAbsoluteRupees;
        rule.ThresholdPercent = request.ThresholdPercent;
        rule.UpdatedAtUtc = DateTime.UtcNow;

        // Re-anchor the reference point whenever threshold tracking (re)starts or the tracked
        // series changes — otherwise a stale reference from a different metal/purity would linger.
        if (request.ThresholdEnabled && (thresholdNewlyEnabled || metalOrPurityChanged))
        {
            var referencePrice = GetCurrentPrice(request.Metal, request.Purity);
            rule.ThresholdReferencePriceInrPerGram = referencePrice;
            rule.ThresholdReferenceSetAtUtc = referencePrice.HasValue ? rule.UpdatedAtUtc : null;
        }
        else if (!request.ThresholdEnabled)
        {
            rule.ThresholdReferencePriceInrPerGram = null;
            rule.ThresholdReferenceSetAtUtc = null;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(rule));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var rule = await db.NotificationRules
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == CurrentUserId, cancellationToken);
        if (rule is null)
        {
            return NotFound();
        }

        db.NotificationRules.Remove(rule);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private decimal? GetCurrentPrice(Metal metal, Purity purity) => rateCache.Current?.GetPrice(metal, purity);

    private static bool TryValidate(UpsertNotificationRuleRequest request, out string? error)
    {
        var validMetalPurity =
            (request.Metal == Metal.Gold && request.Purity is Purity.TwentyTwoK or Purity.TwentyFourK) ||
            (request.Metal == Metal.Silver && request.Purity == Purity.Pure);
        if (!validMetalPurity)
        {
            error = "Silver must use Purity=Pure; Gold must use Purity=TwentyTwoK or TwentyFourK.";
            return false;
        }

        if (!request.DigestEnabled && !request.ThresholdEnabled)
        {
            error = "At least one of DigestEnabled or ThresholdEnabled must be true.";
            return false;
        }

        if (request.DigestEnabled)
        {
            if (request.DigestFrequencyType == DigestFrequencyType.DailyAtTime && request.DigestTimeOfDay is null)
            {
                error = "DigestTimeOfDay is required when DigestFrequencyType is DailyAtTime.";
                return false;
            }

            if (request.DigestFrequencyType == DigestFrequencyType.EveryNHours &&
                (request.DigestIntervalHours is null or <= 0))
            {
                error = "DigestIntervalHours must be a positive number when DigestFrequencyType is EveryNHours.";
                return false;
            }
        }

        if (request.ThresholdEnabled &&
            request.ThresholdAbsoluteRupees is null or <= 0 &&
            request.ThresholdPercent is null or <= 0)
        {
            error = "ThresholdEnabled requires a positive ThresholdAbsoluteRupees and/or ThresholdPercent.";
            return false;
        }

        error = null;
        return true;
    }

    private static NotificationRuleDto ToDto(NotificationRule r) => new(
        r.Id, r.City, r.Metal, r.Purity, r.IsActive,
        r.DigestEnabled, r.DigestFrequencyType, r.DigestTimeOfDay, r.DigestIntervalHours, r.DigestLastSentAtUtc,
        r.ThresholdEnabled, r.ThresholdAbsoluteRupees, r.ThresholdPercent,
        r.ThresholdReferencePriceInrPerGram, r.ThresholdReferenceSetAtUtc,
        r.CreatedAtUtc, r.UpdatedAtUtc);
}
