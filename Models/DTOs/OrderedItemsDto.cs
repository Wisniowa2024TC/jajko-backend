namespace JajkoBackend.Models.DTOs;

public class OrderedItemsDto
{
    public List<MinimalItemDto> Items { get; set; }

    public record MinimalItemDto
    {
        public int productId { get; set; }
        public int quantity { get; set; }
    }
}