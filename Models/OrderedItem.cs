namespace JajkoBackend.Models;

public class OrderedItem
{
    public required int ItemId { get; set; }
    public required int OrderId { get; set; }
    public int Amount { get; set; }
    public double Discount { get; set; }
}