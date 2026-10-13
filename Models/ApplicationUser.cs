using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.AspNetCore.Identity;

namespace JajkoBackend.Models;

public class ApplicationUser : IdentityUser<int>
{
    [MaxLength(64)]
    public required string Name { get; set; }

    [MaxLength(64)]
    public required string Surname { get; set; }

    [ForeignKey(nameof(Order.UserId))]
    public ICollection<Order> Orders { get; set; } = [];
}
