using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Identity;

namespace JajkoBackend.Models;

// Id użytownika, Email i PasswordHash są dziedziczone z IdentityUser<int>
public class ApplicationUser : IdentityUser<int>
{
    [MaxLength(64)]
    public required string Name { get; set; }

    [MaxLength(64)]
    public required string Surname { get; set; }

    public ICollection<Order> Orders { get; set; } = [];
}
