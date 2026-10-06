namespace JajkoBackend.Models;

public class PriceHistory
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
    public Item Item { get; set; }
}
