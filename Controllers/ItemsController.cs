using JajkoBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ItemsController(AppDbContext db)
    {
        _db = db;
    }

    // GET: api/items
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Item>>> GetItems()
    {
        return await _db.Items
            .AsNoTracking()
            .ToListAsync();
    }

    // GET: api/items/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Item>> GetItem(int id)
    {
        var item = await _db.Items
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null)
        {
            return NotFound();
        }

        return item;
    }

    // GET: api/items/5/discount

    [HttpGet("{id}/discount")]
    public async Task<ActionResult<Discount>> GetItemDiscount(int id)
    {
        var itemExists = await _db.Items.AnyAsync(i => i.Id == id);

        if (!itemExists)
        {
            return NotFound();
        }

        var discount = await _db.Discounts
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ItemId == id);

        return discount;
    }

    // POST: api/items
    [HttpPost]
    public async Task<ActionResult<Item>> CreateItem(Item item)
    {
        _db.Items.Add(item);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetItem),
            new { id = item.Id },
            item
        );
    }

    // PUT: api/items/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, Item item)
    {
        if (id != item.Id)
        {
            return BadRequest();
        }

        var existingItem = await _db.Items.FindAsync(id);

        if (existingItem == null)
        {
            return NotFound();
        }

        existingItem.Name = item.Name;
        existingItem.Price = item.Price;
        existingItem.Allergens = item.Allergens;
        existingItem.CategoryId = item.CategoryId;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/items/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _db.Items.FindAsync(id);

        if (item == null)
        {
            return NotFound();
        }

        _db.Items.Remove(item);

        await _db.SaveChangesAsync();

        return NoContent();
    }
}
