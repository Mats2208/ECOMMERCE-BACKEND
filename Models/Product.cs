namespace EcommerceA.Models;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerCompanyUserId { get; set; }   // User.Id con Role=Company
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Nav (opcional, útil para includes)
    public User? OwnerCompanyUser { get; set; }
}