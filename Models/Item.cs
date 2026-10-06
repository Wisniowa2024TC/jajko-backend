namespace JajkoBackend.Models;

public class Item
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
    public string Allergens { get; set; }
}