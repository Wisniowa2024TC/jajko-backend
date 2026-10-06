using JajkoBackend.Models;

namespace JajkoBackend.Services;

public interface IDiscountService
{
    Task<List<Discount>> GetDiscounts();
    Task<Discount?> GetDiscount(int id);
}
