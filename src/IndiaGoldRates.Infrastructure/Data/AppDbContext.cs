using IndiaGoldRates.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IndiaGoldRates.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<NotificationRule> NotificationRules => Set<NotificationRule>();
    public DbSet<RateSnapshot> RateSnapshots => Set<RateSnapshot>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<NotificationRule>(entity =>
        {
            entity.HasIndex(r => new { r.UserId, r.IsActive });
            entity.Property(r => r.ThresholdAbsoluteRupees).HasPrecision(18, 4);
            entity.Property(r => r.ThresholdPercent).HasPrecision(9, 4);
            entity.Property(r => r.ThresholdReferencePriceInrPerGram).HasPrecision(18, 4);
            entity.HasOne(r => r.User)
                .WithMany(u => u.NotificationRules)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RateSnapshot>(entity =>
        {
            entity.HasIndex(s => s.CapturedAtUtc);
            entity.HasIndex(s => new { s.Metal, s.Purity, s.CapturedAtUtc });
            entity.Property(s => s.PriceInrPerGram).HasPrecision(18, 4);
            entity.Property(s => s.SpotPriceUsdPerOz).HasPrecision(18, 4);
            entity.Property(s => s.UsdToInrRate).HasPrecision(18, 6);
        });

        builder.Entity<NotificationLog>(entity =>
        {
            entity.HasIndex(l => l.NotificationRuleId);
            entity.Property(l => l.PriceInrPerGramAtSend).HasPrecision(18, 4);
            entity.HasOne(l => l.NotificationRule)
                .WithMany(r => r.NotificationLogs)
                .HasForeignKey(l => l.NotificationRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
