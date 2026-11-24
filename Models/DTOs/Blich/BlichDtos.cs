using System.ComponentModel.DataAnnotations;

namespace JajkoBackend.Models.DTOs.Blich;

public class StartBlichPaymentRequest
{
    [Required, RegularExpression(@"^\d{6}$", ErrorMessage = "BLIK code must be exactly 6 digits.")]
    public string BlikCode { get; set; } = string.Empty;
}

public record BlichPaymentResponse(
    Guid PaymentId,
    int OrderId,
    string Status,
    decimal Amount,
    DateTime ExpiresAt,
    bool OrderPaid,
    string? FailureReason)
{
    public static BlichPaymentResponse From(BlichPayment p) => new(
        p.Id, p.OrderId, p.Status.ToString(), p.Amount, DateTime.SpecifyKind(p.ExpiresAt, DateTimeKind.Utc),
        p.Status == BlichPaymentStatus.Approved, p.FailureReason);
}

public record MessageResponse(string Message);
