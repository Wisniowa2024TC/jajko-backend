using JajkoBackend.Models;
using JajkoBackend.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("/api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext db;

    public OrdersController(AppDbContext db)
    {
        this.db = db;
    }
    
    [HttpGet("/get/byOrderId/{orderId}")]
    public async Task<IActionResult> Example(int orderId)
    {
        Order? found = db.Orders.FirstOrDefault(order => order.Id == orderId, null);
        if (found == null)
        {
            return NotFound();
        }

        List<OrderedItem> orderedItems = await db.OrderedItems.Where(item => item.OrderId == orderId).ToListAsync();
        OrderDetailsDto orderDetailsDto = new(found, orderedItems);
        return Ok(orderDetailsDto);
    }
}