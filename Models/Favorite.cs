namespace EcommerceA.Models;

public class Favorite
{
    // PK compuesta
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    // Auditoría mínima
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navs (opcionales)
    public User? User { get; set; }
    public Product? Product { get; set; }
}