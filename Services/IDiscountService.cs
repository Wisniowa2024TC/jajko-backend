using JajkoBackend.Models;

namespace JajkoBackend.Services;

public interface IDiscountService
{
    Task<List<Discount>> GetDiscounts();
    Task<Discount?> GetDiscount(int id);
    Task<Discount> CreateDiscount(Discount discount);
    Task<bool> UpdateDiscount(int id, Discount discount);
    Task<bool> DeleteDiscount(int id);
}
