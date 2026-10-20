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
        return await _db.ItemStocks
            .AsNoTracking()
            .ToListAsync();
    }

    // GET: api/itemStocks/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Item>> GetItem(int id)
    {
        var itemStock = await _db.ItemStocks
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

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
            nameof(GetItem),
            new { id = itemStock.Id },
            itemStock
        );
    }

    // PUT: api/itemStocks/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, Item itemStock)
    {
        if (id != itemStock.Id)
        {
            return BadRequest();
        }

        var existingItemStock = await _db.ItemStocks.FindAsync(id);

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
    public async Task<IActionResult> DeleteItem(int id)
    {
        var itemStock = await _db.ItemStocks.FindAsync(id);

        if (itemStock == null)
        {
            return NotFound();
        }

        _db.ItemStocks.Remove(itemStock);

        await _db.SaveChangesAsync();

        return NoContent();
    }
}
