namespace EcommerceA.Models;

public enum Role
{
    AdminRoot = 0,   // super admin (root) – único, seed en Program.cs
    Company   = 1,   // empresa
    Client    = 2    // cliente
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public Role Role { get; set; }

    // Para usuarios empresa (opcional):
    public string? CompanyName { get; set; }

    // Para clientes:
    // Guardamos favoritos como join simple en string (CSV) para no crear otra tabla.
    // Si prefieres tabla, luego lo pasamos a entidad Favorite.
    public string FavoriteProductIdsCsv { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Helpers
    public HashSet<Guid> GetFavoriteIds()
        => FavoriteProductIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(Guid.Parse).ToHashSet();

    public void SetFavoriteIds(IEnumerable<Guid> ids)
        => FavoriteProductIdsCsv = string.Join(',', ids);
}