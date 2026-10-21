using JajkoBackend.Models;
using JajkoBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("/api/cart")]
public class CartController: ControllerBase
{
    private AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CartController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize]
    [HttpPost]
    [Route("/add")]
    public async Task<IActionResult> Add(CartItemDto? itemDto)
    {
        if (!int.TryParse(_userManager.GetUserId(User), out int userId))
        {
            return Unauthorized();
        }; 
        
        if (itemDto == null)
        {
            return BadRequest();
        }
        
        ItemStock? itemStock = await _db.ItemStocks.FirstOrDefaultAsync(stock => stock.ItemId == itemDto.ItemId);
        if (itemStock == null)
        {
            return NotFound();
        }

        if (itemStock.Amount == 0)
        {
            return BadRequest("This item is sold out.");
        }

        Item? item = await _db.Items.FirstOrDefaultAsync(item => item.Id == itemDto.ItemId);
        if (item == null)
        {
            return NotFound();
        }

        _db.CartItems.Add(new CartItem { ItemId = item.Id, Quantity = itemDto.Quantity, UserId = userId });
        await _db.SaveChangesAsync();
        return Ok();
    }
    
}