namespace JajkoBackend.Models;

public enum OrderPaymentStatus
{
    Unpaid = 0,
    Paid = 1
}

public class Order
{
    public int Id { get; set; }
    public required int UserId { get; set; }

    /// <summary>Amount to pay, computed on the server when the order is created.</summary>
    public decimal Total { get; set; }

    public OrderPaymentStatus PaymentStatus { get; set; } = OrderPaymentStatus.Unpaid;
    public DateTime? PaidAt { get; set; }
}
