namespace JajkoBackend.Services.Blich;

public enum BankPaymentState
{
    Pending,
    Approved,
    Failed,
    NotFound
}

/// <summary>Outcome of the bank's initiate call. <see cref="Error"/> carries the bank's message on rejection.</summary>
public record BankInitiateResult(bool Success, Guid? PaymentId, string? Error);

/// <summary>Thrown when the bank can't be reached or answers with a 5xx / unexpected response.</summary>
public class BankUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

public interface IBankBlikClient
{
    /// <summary>Not retried: a retry could create a second pending payment for the same code.</summary>
    Task<BankInitiateResult> InitiateAsync(string blikCode, decimal amount, CancellationToken ct = default);

    Task<BankPaymentState> GetStatusAsync(Guid bankPaymentId, CancellationToken ct = default);
}
