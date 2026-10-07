using JajkoBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace JajkoBackend;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<Discount> Discounts { get; set; }
    public DbSet<ItemStock> ItemStocks { get; set; }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderedItem> OrderedItems => Set<OrderedItem>();
    public DbSet<PriceHistory> PriceHistories { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category
        modelBuilder.Entity<Category>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<Category>()
            .Property(c => c.Name)
            .HasMaxLength(64)
            .IsRequired();

        // Category -> ParentCategory
        modelBuilder.Entity<Category>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentCategory)
            .OnDelete(DeleteBehavior.Restrict);


        // Item
        modelBuilder.Entity<Item>()
            .HasKey(i => i.Id);

        modelBuilder.Entity<Item>()
            .Property(i => i.Name)
            .HasMaxLength(64)
            .IsRequired();

        modelBuilder.Entity<Item>()
            .Property(i => i.Price)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Item>()
            .Property(i => i.Allergens)
            .HasMaxLength(64);

        // Item -> Category
        modelBuilder.Entity<Item>()
            .HasOne<Category>()
            .WithMany()
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);


        // Discount
        modelBuilder.Entity<Discount>()
            .HasKey(d => d.Id);

        // Discount -> Item
        modelBuilder.Entity<Discount>()
            .HasOne<Item>()
            .WithMany()
            .HasForeignKey(d => d.ItemId)
            .OnDelete(DeleteBehavior.Cascade);


        // ItemStock
        modelBuilder.Entity<ItemStock>()
            .HasKey(s => s.ItemId);

        // ItemStock -> Item
        modelBuilder.Entity<ItemStock>()
            .HasOne<Item>()
            .WithOne()
            .HasForeignKey<ItemStock>(s => s.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // PriceHistory
        modelBuilder.Entity<PriceHistory>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<PriceHistory>()
            .Property(p => p.Price)
            .HasPrecision(10, 2);

        // PriceHistory -> Item
        modelBuilder.Entity<PriceHistory>()
            .HasOne<Item>()
            .WithMany()
            .HasForeignKey(p => p.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}