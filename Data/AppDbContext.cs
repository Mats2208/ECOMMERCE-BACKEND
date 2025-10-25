using EcommerceA.Models;
using Microsoft.EntityFrameworkCore;

namespace EcommerceA.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // USER
        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.Email).IsRequired().HasMaxLength(180);
            b.Property(u => u.PasswordHash).IsRequired();
            b.Property(u => u.CompanyName).HasMaxLength(200);
            b.Property(u => u.FavoriteProductIdsCsv).HasMaxLength(4000);
        });

        // PRODUCT
        modelBuilder.Entity<Product>(b =>
        {
            b.HasIndex(p => p.OwnerCompanyUserId);
            b.Property(p => p.Name).IsRequired().HasMaxLength(200);
            b.Property(p => p.Description).HasMaxLength(2000);
            b.Property(p => p.Price).HasPrecision(18, 2);
            b.Property(p => p.Stock).HasDefaultValue(0);
            b.HasOne(p => p.OwnerCompanyUser)
                .WithMany()
                .HasForeignKey(p => p.OwnerCompanyUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // CART
        modelBuilder.Entity<Cart>(b =>
        {
            b.HasIndex(c => new { c.ClientUserId, c.Status });
            b.HasOne(c => c.ClientUser)
                .WithMany()
                .HasForeignKey(c => c.ClientUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Owned collection: Cart.Items -> CartItems table
            b.OwnsMany(c => c.Items, a =>
            {
                a.ToTable("CartItems");
                a.WithOwner().HasForeignKey("CartId");
                a.Property<int>("Id"); // shadow key
                a.HasKey("Id");

                a.Property(i => i.ProductId).IsRequired();
                a.Property(i => i.ProductName).IsRequired().HasMaxLength(200);
                a.Property(i => i.UnitPrice).HasPrecision(18, 2);
                a.Property(i => i.Quantity).IsRequired();
            });
        });
    }
}
