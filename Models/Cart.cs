namespace EcommerceA.Models;

public enum CartStatus
{
    Active = 0,
    CheckedOut = 1,
    Abandoned = 2
}

public class Cart
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ClientUserId { get; set; }         // User.Id con Role=Client
    public CartStatus Status { get; set; } = CartStatus.Active;
    public List<CartItem> Items { get; set; } = new();
    public decimal Total => Items.Sum(i => i.UnitPrice * i.Quantity);
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Nav
    public User? ClientUser { get; set; }
}

// Owned type (no es modelo base)
public class CartItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;  // snapshot
    public decimal UnitPrice { get; set; }               // snapshot
    public int Quantity { get; set; }
}