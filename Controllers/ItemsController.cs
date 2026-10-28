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
        var items = await _db.Items
            .AsNoTracking()
            .ToListAsync();

        if (items == null)
        {
            return NotFound();
        }

        return items;
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

        var now = DateTime.Now;

        var discount = await _db.Discounts
            .FirstOrDefaultAsync(d =>
                d.ItemId == id &&
                d.StartDate <= now &&
                d.EndDate > now);

        if (discount == null)
        {
            return NotFound();
        }

        return discount;
    }

    // GET: api/items/5/lowest-price

    [HttpGet("{id}/lowest-price")]
    public async Task<ActionResult<decimal>> GetLowestPrice(int id)
    {
        var itemExists = await _db.Items
            .AnyAsync(i => i.Id == id);

        if (!itemExists)
        {
            return NotFound();
        }

        var thirtyDaysAgo = DateTime.Now.AddDays(-30);

        var lowestPrice = await _db.PriceHistories
            .Where(p =>
                p.ItemId == id &&
                p.Date >= thirtyDaysAgo)
            .MinAsync(p => p.Price);

        return lowestPrice;
    }

    // POST: api/items
    [HttpPost]
    public async Task<ActionResult<Item>> CreateItem(Item item)
    {
        _db.Items.Add(item);

        await _db.SaveChangesAsync();

        var priceHistory = new PriceHistory
        {
            ItemId = item.Id,
            Price = item.Price,
            Date = DateTime.Now
        };

        _db.PriceHistories.Add(priceHistory);

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
        existingItem.ReferencePrice = item.ReferencePrice;
        existingItem.Allergens = item.Allergens;
        existingItem.CategoryId = item.CategoryId;

        if (existingItem.Price != item.Price)
        {
            var priceHistory = new PriceHistory
            {
                Item = existingItem,
                Price = item.Price,
                Date = DateTime.Now
            };

            _db.PriceHistories.Add(priceHistory);
        }

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
