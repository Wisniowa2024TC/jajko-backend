using JajkoBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderedItem> OrderedItems => Set<OrderedItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Tabele Identity (AspNetUsers, AspNetRoles, ...) są konfigurowane w klasie bazowej
        base.OnModelCreating(builder);

        builder.Entity<OrderedItem>().HasKey(oi => new { oi.OrderId, oi.ItemId });
    }
}
