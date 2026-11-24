using JajkoBackend.Models;

namespace JajkoBackend.Services.Blich;

public enum StartOutcome
{
    Started,
    OrderNotFound,
    OrderAlreadyPaid,
    InvalidOrderTotal,
    ActivePaymentExists,
    BankRejected
}

public record StartResult(StartOutcome Outcome, BlichPayment? Payment = null, string? Message = null);

public interface IBlichPaymentService
{
    /// <exception cref="BankUnavailableException">The bank couldn't be reached; the payment row is marked Failed.</exception>
    Task<StartResult> StartAsync(int userId, int orderId, string blikCode, CancellationToken ct = default);

    /// <summary>The caller's payment, refreshed from the bank when still pending. Null if missing or not theirs.</summary>
    Task<BlichPayment?> GetAsync(int userId, Guid paymentId, CancellationToken ct = default);

    /// <summary>Null if the order doesn't exist or isn't the caller's.</summary>
    Task<List<BlichPayment>?> ListForOrderAsync(int userId, int orderId, CancellationToken ct = default);

    /// <summary>The only place a payment changes state after creation. Safe to call concurrently.</summary>
    Task<BlichPayment?> RefreshAsync(Guid paymentId, CancellationToken ct = default);
}
