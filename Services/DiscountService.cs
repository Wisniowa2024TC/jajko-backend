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
        var now = DateTime.Now;

        return await _db.Discounts
            .AsNoTracking()
            .Where(d => d.StartDate <= now && d.EndDate > now)
            .ToListAsync();
    }

    public async Task<Discount?> GetDiscount(int id)
    {
        var now = DateTime.Now;

        return await _db.Discounts
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.Id == id &&
                d.StartDate <= now &&
                d.EndDate > now);
    }


    public async Task<Discount> CreateDiscount(Discount discount)
    {
        _db.Discounts.Add(discount);
        await _db.SaveChangesAsync();

        return discount;
    }

    public async Task<bool> UpdateDiscount(int id, Discount discount)
    {
        var existing = await _db.Discounts.FindAsync(id);

        if (existing == null)
            return false;

        existing.ItemId = discount.ItemId;
        existing.StartDate = discount.StartDate;
        existing.EndDate = discount.EndDate;
        existing.Value = discount.Value;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteDiscount(int id)
    {
        var discount = await _db.Discounts.FindAsync(id);

        if (discount == null)
            return false;

        _db.Discounts.Remove(discount);
        await _db.SaveChangesAsync();

        return true;
    }


}
