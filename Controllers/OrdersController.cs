using System.Security.Claims;
using JajkoBackend.Models;
using JajkoBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using static JajkoBackend.Helpers.ClaimsPrincipalExtensions;
using static JajkoBackend.Helpers.DiscountHelper;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("/api/orders")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext db;
    private readonly UserManager<ApplicationUser> _userManager;
    
    public OrdersController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        this.db = db;
        _userManager = userManager;
    }

    [Authorize]
    [HttpPost("order")]
    public async Task<IActionResult> Order()
    {
        if (!int.TryParse(_userManager.GetUserId(User), out int userId))
        {
            return Unauthorized();
        };

        List<CartItem> cartItems = await db.CartItems.Where(item => item.UserId == userId).ToListAsync();
        
        if (cartItems.Count == 0)
        {
            return NotFound("The user's cart is empty.");
        }
        
        Order order = new Order { UserId = userId };
        
        await db.Orders.AddAsync(order);
        
        await db.SaveChangesAsync();
        
        
        foreach (var item in cartItems)
        {
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

            await db.OrderedItems.AddAsync(new OrderedItem{Amount = item.Quantity, Discount = discountValue, ItemId = item.ItemId, OrderId = order.Id});
        }

        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("get")]
    [Authorize]
    public async Task<IActionResult> GetForUser()
    {
        if (!int.TryParse(_userManager.GetUserId(User), out int userId))
        {
            return Unauthorized();
        };

        List<int> orderIds = db.Orders.Select(item => item.Id).ToList();
        List<OrderedItem>  orderedItems = await db.OrderedItems.Where(item => orderIds.Contains(item.OrderId)).ToListAsync();

        return Ok();
        /*OrderDetailsDto orderDetailsDto = new
        {

        }*/
    }
        
    
    [HttpGet("get/byOrderId/{orderId}")]
    public async Task<IActionResult> GetByOrderId(int orderId)
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