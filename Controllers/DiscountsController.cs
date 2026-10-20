using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JajkoBackend.Models;
using JajkoBackend.Services;

namespace JajkoBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountService _discountService;

    public DiscountsController(IDiscountService discountService)
    {
        _discountService = discountService;
    }

    // GET: api/discounts
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Discount>>> GetDiscounts()
    {
        var discounts = await _discountService.GetDiscounts();

        return Ok(discounts);
    }

    // GET: api/discounts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Discount>> GetDiscount(int id)
    {
        var discount = await _discountService.GetDiscount(id);

        if (discount == null)
            return NotFound();

        return Ok(discount);
    }

    // POST: api/discounts
    [HttpPost]
    public async Task<ActionResult<Discount>> CreateDiscount(
        Discount discount)
    {
        var createdDiscount =
            await _discountService.CreateDiscount(discount);

        return CreatedAtAction(
            nameof(GetDiscount),
            new { id = createdDiscount.Id },
            createdDiscount
        );
    }

    // PUT: api/discounts/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDiscount(
        int id, Discount discount)
    {
        if (id != discount.Id)
            return BadRequest();

        var updated =
            await _discountService.UpdateDiscount(id, discount);

        if (!updated)
            return NotFound();

        return NoContent();
    }

    // DELETE: api/discounts/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDiscount(int id)
    {
        var deleted = await _discountService.DeleteDiscount(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}