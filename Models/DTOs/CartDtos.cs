namespace JajkoBackend.Models.DTOs;

public class CartItemDto
{
    public int ItemId { get; set; }
    public int Quantity { get; set; }
}

public class CartItemUpdateDto
{
    public int Id { get; set; }
    public int Quantity { get; set; }
}