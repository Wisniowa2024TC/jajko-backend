namespace JajkoBackend.Models;

public class Order
{
    public int Id { get; set; }
    public required int UserId { get; set; }
}