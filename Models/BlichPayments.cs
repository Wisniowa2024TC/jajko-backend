namespace JajkoBackend.Models;

public enum BlichPaymentStatus
{
    Initiating = 0,
    Pending = 1,
    Approved = 2,
    Failed = 3,
    Expired = 4
}

public class BlichPayment
{
    public Guid Id { get; set; }                  // our id, returned to the frontend
    public Guid? BankPaymentId { get; set; }      // the bank's paymentId
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public required string MerchantId { get; set; }
    public BlichPaymentStatus Status { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }       
    public DateTime ExpiresAt { get; set; }       // UTC: created + bank window + grace
    public DateTime? LastPolledAt { get; set; }   
    public DateTime? CompletedAt { get; set; }    
}
