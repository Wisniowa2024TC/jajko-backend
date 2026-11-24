using JajkoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Services.Blich;

/// <summary>
/// The bank has no webhooks, so pending payments are polled here. This is what marks an order paid
/// when the shopper closes the checkout tab right after approving in the banking app.
/// </summary>
public class BlichReconciliationService(
    IServiceScopeFactory scopes,
    TimeProvider time,
    ILogger<BlichReconciliationService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan MinRepoll = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StuckInitiating = TimeSpan.FromSeconds(30);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "BLIK reconciliation pass failed.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var payments = scope.ServiceProvider.GetRequiredService<IBlichPaymentService>();

        var now = time.GetUtcNow().UtcDateTime;

        // A crash between our insert and the bank call leaves an Initiating row that would block the order.
        var stuckBefore = now - StuckInitiating;
        await db.BlichPayments
            .Where(p => p.Status == BlichPaymentStatus.Initiating && p.CreatedAt < stuckBefore)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, BlichPaymentStatus.Failed)
                .SetProperty(p => p.FailureReason, "Interrupted before the bank confirmed the request.")
                .SetProperty(p => p.CompletedAt, now), ct);

        var polledBefore = now - MinRepoll;
        var ids = await db.BlichPayments.AsNoTracking()
            .Where(p => p.Status == BlichPaymentStatus.Pending
                        && (p.LastPolledAt == null || p.LastPolledAt < polledBefore))
            .OrderBy(p => p.CreatedAt)
            .Select(p => p.Id)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var id in ids)
        {
            try
            {
                await payments.RefreshAsync(id, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to reconcile BLIK payment {PaymentId}.", id);
            }
        }
    }
}
