namespace JajkoBackend.Models;

public class Item
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Allergens { get; set; }
}