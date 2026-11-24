using JajkoBackend.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JajkoBackend.Services.Blich;

public class BlichPaymentService(
    AppDbContext db,
    IBankBlikClient bank,
    IOptions<BankOptions> options,
    TimeProvider time,
    ILogger<BlichPaymentService> logger) : IBlichPaymentService
{
    private static readonly TimeSpan MinPollInterval = TimeSpan.FromSeconds(1);
    private readonly BankOptions _options = options.Value;

    public async Task<StartResult> StartAsync(int userId, int orderId, string blikCode, CancellationToken ct = default)
    {
        var order = await db.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, ct);

        if (order is null)
        {
            return new StartResult(StartOutcome.OrderNotFound);
        }

        if (order.PaymentStatus == OrderPaymentStatus.Paid)
        {
            return new StartResult(StartOutcome.OrderAlreadyPaid, Message: "Order is already paid.");
        }

        if (order.Total <= 0)
        {
            return new StartResult(StartOutcome.InvalidOrderTotal, Message: "Order has nothing to pay.");
        }

        var now = time.GetUtcNow().UtcDateTime;
        var payment = new BlichPayment
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            UserId = userId,
            Amount = order.Total,
            MerchantId = _options.MerchantId,
            Status = BlichPaymentStatus.Initiating,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(_options.PaymentTimeoutSeconds)
        };

        db.BlichPayments.Add(payment);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteErrorCode: 19 })
        {
            // The filtered unique index on OrderId: another payment is already in flight.
            db.Entry(payment).State = EntityState.Detached;
            return new StartResult(StartOutcome.ActivePaymentExists,
                Message: "This order already has a payment in progress.");
        }

        BankInitiateResult result;
        try
        {
            result = await bank.InitiateAsync(blikCode, payment.Amount, ct);
        }
        catch (BankUnavailableException)
        {
            await FinishAsync(payment.Id, BlichPaymentStatus.Initiating, BlichPaymentStatus.Failed,
                "Bank unavailable.", CancellationToken.None);
            throw;
        }

        if (!result.Success)
        {
            await FinishAsync(payment.Id, BlichPaymentStatus.Initiating, BlichPaymentStatus.Failed,
                Truncate(result.Error), CancellationToken.None);
            return new StartResult(StartOutcome.BankRejected, Message: result.Error);
        }

        payment.BankPaymentId = result.PaymentId;
        payment.Status = BlichPaymentStatus.Pending;
        await db.SaveChangesAsync(CancellationToken.None);

        return new StartResult(StartOutcome.Started, payment);
    }

    public async Task<BlichPayment?> GetAsync(int userId, Guid paymentId, CancellationToken ct = default)
    {
        var owned = await db.BlichPayments.AsNoTracking()
            .AnyAsync(p => p.Id == paymentId && p.UserId == userId, ct);

        return owned ? await RefreshAsync(paymentId, ct) : null;
    }

    public async Task<List<BlichPayment>?> ListForOrderAsync(int userId, int orderId, CancellationToken ct = default)
    {
        if (!await db.Orders.AsNoTracking().AnyAsync(o => o.Id == orderId && o.UserId == userId, ct))
        {
            return null;
        }

        return await db.BlichPayments.AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<BlichPayment?> RefreshAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await LoadAsync(paymentId, ct);
        if (payment is null || payment.Status != BlichPaymentStatus.Pending || payment.BankPaymentId is null)
        {
            return payment;
        }

        var now = time.GetUtcNow().UtcDateTime;
        if (payment.LastPolledAt is { } last && now - last < MinPollInterval && now <= payment.ExpiresAt)
        {
            return payment;
        }

        await db.BlichPayments.Where(p => p.Id == paymentId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastPolledAt, now), ct);

        BankPaymentState state;
        try
        {
            state = await bank.GetStatusAsync(payment.BankPaymentId.Value, ct);
        }
        catch (BankUnavailableException ex)
        {
            logger.LogWarning(ex, "Bank unavailable while checking BLIK payment {PaymentId}.", paymentId);
            return await LoadAsync(paymentId, ct);
        }

        switch (state)
        {
            // Approved wins even after our deadline: the money has already moved.
            case BankPaymentState.Approved:
                await ApproveAsync(payment, now, ct);
                break;
            case BankPaymentState.Failed:
                await FinishAsync(paymentId, BlichPaymentStatus.Pending, BlichPaymentStatus.Failed,
                    "Rejected, declined or timed out at the bank.", ct);
                break;
            case BankPaymentState.NotFound:
                await FinishAsync(paymentId, BlichPaymentStatus.Pending, BlichPaymentStatus.Failed,
                    "The bank does not know this payment.", ct);
                break;
            case BankPaymentState.Pending when now > payment.ExpiresAt:
                await FinishAsync(paymentId, BlichPaymentStatus.Pending, BlichPaymentStatus.Expired,
                    "Not approved in time.", ct);
                break;
        }

        return await LoadAsync(paymentId, ct);
    }

    private Task<BlichPayment?> LoadAsync(Guid paymentId, CancellationToken ct) =>
        db.BlichPayments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == paymentId, ct);

    // Pending -> Approved and Order -> Paid commit together; the conditional update makes it idempotent.
    private async Task ApproveAsync(BlichPayment payment, DateTime now, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var changed = await db.BlichPayments
            .Where(p => p.Id == payment.Id && p.Status == BlichPaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, BlichPaymentStatus.Approved)
                .SetProperty(p => p.CompletedAt, now), ct);

        if (changed == 1)
        {
            await db.Orders.Where(o => o.Id == payment.OrderId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.PaymentStatus, OrderPaymentStatus.Paid)
                    .SetProperty(o => o.PaidAt, now), ct);
        }

        await tx.CommitAsync(ct);
    }

    private Task<int> FinishAsync(Guid paymentId, BlichPaymentStatus from, BlichPaymentStatus to, string? reason,
        CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        return db.BlichPayments
            .Where(p => p.Id == paymentId && p.Status == from)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, to)
                .SetProperty(p => p.FailureReason, reason)
                .SetProperty(p => p.CompletedAt, now), ct);
    }

    private static string? Truncate(string? s) => s is { Length: > 200 } ? s[..200] : s;
}
