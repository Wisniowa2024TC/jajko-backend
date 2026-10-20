using JajkoBackend.Models;

namespace JajkoBackend.Helpers;

public static class DiscountHelper
{
    public static bool IsActive(this Discount discount, DateTime dateTime)
    {
        return discount.StartDate <= dateTime && dateTime <= discount.EndDate;
    }

    public static bool IsActive(this Discount discount)
    {
        return IsActive(discount, DateTime.Now);
    }
}