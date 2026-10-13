using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Models;

[PrimaryKey(nameof(OrderId), nameof(ItemId))]
public class OrderedItem
{
    public required int ItemId { get; set; }
    public required int OrderId { get; set; }
    public int Amount { get; set; }
    public double Discount { get; set; }
    public string? Notes { get; set; }
}