using IndiaGoldRates.Core.Entities;
using IndiaGoldRates.Core.Enums;
using IndiaGoldRates.Core.Interfaces;
using IndiaGoldRates.Core.Models;
using IndiaGoldRates.Infrastructure.Data;
using IndiaGoldRates.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndiaGoldRates.Api.Tests;

file class FakeNotificationSender : INotificationSender
{
    public List<NotificationMessage> Sent { get; } = [];

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}

public class NotificationRuleEvaluatorTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ApplicationUser CreateUser(AppDbContext db)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "alice@example.com",
            UserName = "alice@example.com"
        };
        db.Users.Add(user);
        return user;
    }

    private static CurrentRatesView MakeRates(decimal gold24K = 12000m, decimal gold22K = 11000m, decimal silver = 180m) =>
        new(new MetalRateView(gold22K), new MetalRateView(gold24K), new MetalRateView(silver), false, DateTime.UtcNow);

    [Fact]
    public async Task ThresholdRule_FiresWhenAbsoluteRupeeDeltaCrossed_AndResetsReference()
    {
        using var db = CreateDb();
        var user = CreateUser(db);
        var rule = new NotificationRule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            City = City.Hyderabad,
            Metal = Metal.Gold,
            Purity = Purity.TwentyFourK,
            IsActive = true,
            ThresholdEnabled = true,
            ThresholdAbsoluteRupees = 100m,
            ThresholdReferencePriceInrPerGram = 12000m,
            ThresholdReferenceSetAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        db.NotificationRules.Add(rule);
        await db.SaveChangesAsync();

        var sender = new FakeNotificationSender();
        var evaluator = new NotificationRuleEvaluator(db, sender, NullLogger<NotificationRuleEvaluator>.Instance);

        await evaluator.EvaluateAsync(MakeRates(gold24K: 12150m), DateTime.UtcNow, CancellationToken.None);

        Assert.Single(sender.Sent);
        Assert.Equal(12150m, rule.ThresholdReferencePriceInrPerGram);
        Assert.NotNull(rule.ThresholdLastTriggeredAtUtc);

        var log = Assert.Single(db.NotificationLogs);
        Assert.Equal(NotificationStatus.Sent, log.Status);
        Assert.Equal(NotificationTriggerType.Threshold, log.TriggerType);
    }

    [Fact]
    public async Task ThresholdRule_DoesNotFire_WhenDeltaBelowThreshold()
    {
        using var db = CreateDb();
        var user = CreateUser(db);
        var rule = new NotificationRule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            City = City.Mumbai,
            Metal = Metal.Silver,
            Purity = Purity.Pure,
            IsActive = true,
            ThresholdEnabled = true,
            ThresholdAbsoluteRupees = 10m,
            ThresholdReferencePriceInrPerGram = 180m,
            ThresholdReferenceSetAtUtc = DateTime.UtcNow.AddHours(-1)
        };
        db.NotificationRules.Add(rule);
        await db.SaveChangesAsync();

        var sender = new FakeNotificationSender();
        var evaluator = new NotificationRuleEvaluator(db, sender, NullLogger<NotificationRuleEvaluator>.Instance);

        await evaluator.EvaluateAsync(MakeRates(silver: 185m), DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(sender.Sent);
        Assert.Equal(180m, rule.ThresholdReferencePriceInrPerGram);
    }

    [Fact]
    public async Task ThresholdRule_RespectsCooldown_AfterTriggering()
    {
        using var db = CreateDb();
        var user = CreateUser(db);
        var rule = new NotificationRule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            City = City.Delhi,
            Metal = Metal.Gold,
            Purity = Purity.TwentyFourK,
            IsActive = true,
            ThresholdEnabled = true,
            ThresholdAbsoluteRupees = 50m,
            ThresholdReferencePriceInrPerGram = 12000m,
            ThresholdLastTriggeredAtUtc = DateTime.UtcNow.AddMinutes(-5) // inside the 15-minute cooldown
        };
        db.NotificationRules.Add(rule);
        await db.SaveChangesAsync();

        var sender = new FakeNotificationSender();
        var evaluator = new NotificationRuleEvaluator(db, sender, NullLogger<NotificationRuleEvaluator>.Instance);

        await evaluator.EvaluateAsync(MakeRates(gold24K: 12200m), DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task ThresholdRule_BackfillsNullReference_WithoutFiringFirstCycle()
    {
        using var db = CreateDb();
        var user = CreateUser(db);
        var rule = new NotificationRule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            City = City.Bangalore,
            Metal = Metal.Gold,
            Purity = Purity.TwentyTwoK,
            IsActive = true,
            ThresholdEnabled = true,
            ThresholdAbsoluteRupees = 50m,
            ThresholdReferencePriceInrPerGram = null // cache was empty when the rule was created
        };
        db.NotificationRules.Add(rule);
        await db.SaveChangesAsync();

        var sender = new FakeNotificationSender();
        var evaluator = new NotificationRuleEvaluator(db, sender, NullLogger<NotificationRuleEvaluator>.Instance);

        await evaluator.EvaluateAsync(MakeRates(gold22K: 11000m), DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(sender.Sent);
        Assert.Equal(11000m, rule.ThresholdReferencePriceInrPerGram);
    }

    [Fact]
    public async Task DigestRule_EveryNHours_FiresOnceThenWaitsForNextInterval()
    {
        using var db = CreateDb();
        var user = CreateUser(db);
        var rule = new NotificationRule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            City = City.Hyderabad,
            Metal = Metal.Silver,
            Purity = Purity.Pure,
            IsActive = true,
            DigestEnabled = true,
            DigestFrequencyType = DigestFrequencyType.EveryNHours,
            DigestIntervalHours = 6
        };
        db.NotificationRules.Add(rule);
        await db.SaveChangesAsync();

        var sender = new FakeNotificationSender();
        var evaluator = new NotificationRuleEvaluator(db, sender, NullLogger<NotificationRuleEvaluator>.Instance);

        var now = DateTime.UtcNow;
        await evaluator.EvaluateAsync(MakeRates(), now, CancellationToken.None);
        Assert.Single(sender.Sent);

        // One hour later — inside the 6h interval, should not fire again.
        await evaluator.EvaluateAsync(MakeRates(), now.AddHours(1), CancellationToken.None);
        Assert.Single(sender.Sent);

        // Seven hours later — interval elapsed, should fire again.
        await evaluator.EvaluateAsync(MakeRates(), now.AddHours(7), CancellationToken.None);
        Assert.Equal(2, sender.Sent.Count);
    }

    [Fact]
    public async Task InactiveRule_IsNeverEvaluated()
    {
        using var db = CreateDb();
        var user = CreateUser(db);
        var rule = new NotificationRule
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            City = City.Hyderabad,
            Metal = Metal.Gold,
            Purity = Purity.TwentyFourK,
            IsActive = false,
            ThresholdEnabled = true,
            ThresholdAbsoluteRupees = 1m,
            ThresholdReferencePriceInrPerGram = 12000m
        };
        db.NotificationRules.Add(rule);
        await db.SaveChangesAsync();

        var sender = new FakeNotificationSender();
        var evaluator = new NotificationRuleEvaluator(db, sender, NullLogger<NotificationRuleEvaluator>.Instance);

        await evaluator.EvaluateAsync(MakeRates(gold24K: 20000m), DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(sender.Sent);
    }
}
