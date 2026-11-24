using System.ComponentModel.DataAnnotations;

namespace JajkoBackend.Services.Blich;

public class BankOptions
{
    public const string SectionName = "Bank";

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, MaxLength(64)]
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>Bank pending window (2 min) plus a small grace period.</summary>
    [Range(30, 600)]
    public int PaymentTimeoutSeconds { get; set; } = 135;
}
