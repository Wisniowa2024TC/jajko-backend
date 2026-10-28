using JajkoBackend.Models;
using JajkoBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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
    [Route("add")]
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
        
        CartItem? existingCartItem = await _db.CartItems.FirstOrDefaultAsync(item => item.UserId == userId &&  item.ItemId == itemDto.ItemId);
        if (existingCartItem != null)
        {
            existingCartItem.Quantity += itemDto.Quantity;
            await _db.SaveChangesAsync();
            return Ok(existingCartItem.Id);
        }

        Item? item = await _db.Items.FirstOrDefaultAsync(item => item.Id == itemDto.ItemId);
        if (item == null)
        {
            return NotFound();
        }

        CartItem cartItem = new CartItem { ItemId = item.Id, Quantity = itemDto.Quantity, UserId = userId };
        _db.CartItems.Add(cartItem);
        await _db.SaveChangesAsync();
        int cartItemId = cartItem.Id;
        return Ok(cartItemId);
    }

    public IActionResult CartItemNotFound() => NotFound("No cart item with this id.");
    
    [Authorize]
    [HttpDelete]
    [Route("remove/{cartItemId}")]
    public async Task<IActionResult> Remove(int cartItemId)
    {
        if (!int.TryParse(_userManager.GetUserId(User), out int userId))
        {
            return Unauthorized();
        }; 
        
        CartItem? cartItem = await _db.CartItems.FirstOrDefaultAsync(item => item.Id == cartItemId);
        if (cartItem == null)
        {
            return CartItemNotFound();
        }
        
        if (cartItem.UserId != userId)
        {
            return Unauthorized();
        }

        _db.CartItems.Remove(cartItem);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [Authorize]
    [HttpPut]
    [Route("update")]
    public async Task<IActionResult> Update(CartItemUpdateDto itemDto)
    {
        if (!int.TryParse(_userManager.GetUserId(User), out int userId))
        {
            return Unauthorized();
        }; 
        
        CartItem? item = await _db.CartItems.FirstOrDefaultAsync(item => item.Id == itemDto.Id);
        if (item == null)
        {
            return CartItemNotFound();
        }

        if (item.UserId != userId)
        {
            return Unauthorized();
        }

        item.Quantity = itemDto.Quantity;
        await _db.SaveChangesAsync();
        return Ok();
    }
}