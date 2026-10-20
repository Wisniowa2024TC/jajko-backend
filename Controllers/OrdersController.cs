using System.Security.Claims;
using JajkoBackend.Models;
using JajkoBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static JajkoBackend.Helpers.ClaimsPrincipalExtensions;

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
    [HttpPost("/order")]
    public async Task<IActionResult> Order(OrderedItemsDto orderedItemsDto)
    {
        if (!int.TryParse(_userManager.GetUserId(User), out int userId))
        {
            return Unauthorized();
        }; 
    }
    
        
    
    [HttpGet("/get/byOrderId/{orderId}")]
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