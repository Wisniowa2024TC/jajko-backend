namespace JajkoBackend.Models.DTOs;

public record OrderDetailsDto(Order Order, List<OrderedItem> OrderedItems);