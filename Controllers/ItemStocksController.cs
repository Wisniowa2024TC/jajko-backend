using JajkoBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemStocksController : ControllerBase
{
    private readonly AppDbContext _db;

    public ItemStocksController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/itemStocks
    [HttpGet]
    public async Task<ActionResult<List<Item>>> GetItemStocks()
    {
       var itemStocks = await _db.ItemStocks
            .AsNoTracking()
            .ToListAsync();

        if (itemStocks == null)
        {
            return NotFound();
        }

        return itemStocks;
    }

    // GET: api/itemStocks/5
    [HttpGet("{itemId}")]
    public async Task<ActionResult<ItemStock>> GetItemStock(int itemId)
    {
        var itemStock = await _db.ItemStocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ItemId == itemId);

        if (itemStock == null)
        {
            return NotFound();
        }

        return itemStock;
    }

    // POST: api/itemStocks
    [HttpPost]
    public async Task<ActionResult<Item>> CreateItem(Item itemStock)
    {
        _db.ItemStocks.Add(itemStock);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetItemStock),
            new { itemId = itemStock.ItemId },
            itemStock
        );
    }

    // PUT: api/itemStocks/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int itemId, Item itemStock)
    {
        if (itemId != itemStock.ItemId)
        {
            return BadRequest();
        }

        var existingItemStock = await _db.ItemStocks.FindAsync(itemId);

        if (existingItemStock == null)
        {
            return NotFound();
        }

        existingItemStock.Amount = itemStock.Amount;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/itemStocks/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int itemId)
    {
        var itemStock = await _db.ItemStocks.FindAsync(itemId);

        if (itemStock == null)
        {
            return NotFound();
        }

        _db.ItemStocks.Remove(itemStock);

        await _db.SaveChangesAsync();

        return NoContent();
    }
}
