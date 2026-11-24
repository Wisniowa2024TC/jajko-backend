using JajkoBackend.Models;
using JajkoBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("/api/orders")]
public class OrdersController(AppDbContext db, UserManager<ApplicationUser> userManager) : ControllerBase
{
    [Authorize]
    [HttpPost("order")]
    public async Task<IActionResult> Order()
    {
        if (!int.TryParse(userManager.GetUserId(User), out int userId))
        {
            return Unauthorized();
        };

        List<CartItem> cartItems = await db.CartItems.Where(item => item.UserId == userId).ToListAsync();
        
        if (cartItems.Count == 0)
        {
            return NotFound("The user's cart is empty.");
        }
        
        List<ItemStock> itemStocks = await db.ItemStocks.Where(stock => cartItems.Select(item => item.ItemId).Contains(stock.ItemId)).ToListAsync();


        foreach (var item in cartItems)
        {
            ItemStock itemStock = itemStocks.First(stock => item.ItemId == stock.ItemId);
            if (item.Quantity > itemStock.Amount)
            {
                return BadRequest($"Insufficient quantity. Attempting to order {item.Quantity} of item #{item.ItemId}, when there is only {itemStock.Amount} of it.");
            }
        }

        Order order = new Order { UserId = userId };

        // Save first so order.Id is generated before the ordered items reference it.
        await db.Orders.AddAsync(order);
        await db.SaveChangesAsync();

        Dictionary<int, decimal> prices = await db.Items
            .Where(i => cartItems.Select(c => c.ItemId).Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.Price);
        decimal total = 0;

        foreach (var item in cartItems)
        {
            ItemStock itemStock = itemStocks.First(stock => item.ItemId == stock.ItemId);
            double discountValue;
            Discount? discount = await db.Discounts.
                FirstOrDefaultAsync(d => d.ItemId == item.ItemId &&  d.ItemId == item.ItemId && d.StartDate <= DateTime.Now && DateTime.Now <= d.EndDate);
            if (discount == null)
            {
                discountValue = 0;
            }
            else
            {
                discountValue = discount.Value;
            }

            total += Math.Round(prices[item.ItemId] * item.Quantity * (decimal)(1 - discountValue),
                2, MidpointRounding.AwayFromZero);
            itemStock.Amount -= item.Quantity;
            await db.OrderedItems.AddAsync(new OrderedItem{Amount = item.Quantity, Discount = discountValue, ItemId = item.ItemId, OrderId = order.Id});
        }

        order.Total = total;
        await db.CartItems.Where(item => item.UserId == userId).ForEachAsync(item => db.Remove(item));
        await db.SaveChangesAsync();
        return Ok(order.Id);
    }

    [HttpGet("get")]
    [Authorize]
    public async Task<IActionResult> GetForUser()
    {
        if (!int.TryParse(userManager.GetUserId(User), out int userId))
        {
            return Unauthorized();
        };

        List<Order> orders = await db.Orders.Where(order => order.UserId == userId).ToListAsync();
        List<OrderDetailsDto> result = new();
        foreach (Order order in orders)
        {
            List<OrderedItem> orderedItems = await db.OrderedItems.Where(item => item.OrderId == order.Id).ToListAsync();
            result.Add(new OrderDetailsDto(order, orderedItems));
        }
        
        return Ok(result);
    }
        
    
    [HttpGet("get/byOrderId/{orderId}")]
    public async Task<IActionResult> GetByOrderId(int orderId)
    {
        Order? found = db.Orders.FirstOrDefault(order => order.Id == orderId);
        if (found == null)
        {
            return NotFound();
        }

        List<OrderedItem> orderedItems = await db.OrderedItems.Where(item => item.OrderId == orderId).ToListAsync();
        OrderDetailsDto orderDetailsDto = new(found, orderedItems);
        return Ok(orderDetailsDto);
    }
}