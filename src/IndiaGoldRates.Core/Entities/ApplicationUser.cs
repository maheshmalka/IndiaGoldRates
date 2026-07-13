using Microsoft.AspNetCore.Identity;

namespace IndiaGoldRates.Core.Entities;

/// <summary>External-login-only user (Google/Microsoft) — no password hash is ever set.</summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<NotificationRule> NotificationRules { get; set; } = new List<NotificationRule>();
}
