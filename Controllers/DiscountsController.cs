using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JajkoBackend.Models;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscountsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DiscountsController(AppDbContext db)
    {
        _db = db;
    }


    // GET: api/discounts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Discount>>> GetDiscounts()
    {
        return await _db.Discounts
            .AsNoTracking()
            .ToListAsync();
    }

    // GET: api/discounts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Discount>> GetDiscount(int id)
    {
        var discount = await _db.Discounts
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (discount == null)
        {
            return NotFound();
        }

        return discount;
    }

    // POST: api/discounts
    [HttpPost]
    public async Task<ActionResult<Discount>> CreateDiscount(Discount discount)
    {
        _db.Discounts.Add(discount);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetDiscount),
            new { id = discount.Id },
            discount
        );
    }

    // PUT: api/discounts/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDiscount(int id, Discount discount)
    {
        if (id != discount.Id)
        {
            return BadRequest();
        }

        var existingDiscount = await _db.Discounts.FindAsync(id);

        if (existingDiscount == null)
        {
            return NotFound();
        }

        existingDiscount.ItemId = discount.ItemId;
        existingDiscount.StartDate = discount.StartDate;
        existingDiscount.EndDate = discount.EndDate;
        existingDiscount.Value = discount.Value;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/discounts/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDiscount(int id)
    {
        var discount = await _db.Discounts.FindAsync(id);

        if (discount == null)
        {
            return NotFound();
        }

        _db.Discounts.Remove(discount);

        await _db.SaveChangesAsync();

        return NoContent();
    }
}