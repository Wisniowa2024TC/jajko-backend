using JajkoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend.Services;

public class DiscountService : IDiscountService
{
    private readonly AppDbContext _db;

    public DiscountService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Discount>> GetDiscounts()
    {
        return await _db.Discounts
            .Where(d => d.EndDate > DateTime.Now)
            .ToListAsync();
    }

    public async Task<Discount?> GetDiscount(int id)
    {
        return await _db.Discounts
            .Where(d => d.EndDate > DateTime.Now)
            .FirstOrDefaultAsync(d => d.Id == id);
    }
}
