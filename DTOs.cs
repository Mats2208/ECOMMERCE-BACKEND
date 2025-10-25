using EcommerceA.Models;

namespace EcommerceA.DTOs;

// Auth
public record RegisterDto(string Email, string Password, Role Role, string? CompanyName);
public record LoginDto(string Email, string Password);
public record AuthResponseDto(string Token, Guid UserId, Role Role, bool IsRoot);

// Products
public record ProductCreateDto(string Name, string? Description, decimal Price, int Stock);
public record ProductUpdateDto(string? Name, string? Description, decimal? Price, int? Stock, bool? IsActive);
public record ProductDto(Guid Id, string Name, string? Description, decimal Price, int Stock, bool IsActive, Guid OwnerCompanyUserId);

// Cart
public record AddToCartDto(Guid ProductId, int Quantity);
public record RemoveFromCartDto(Guid ProductId, int Quantity);
public record CartItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
public record CartDto(Guid Id, Guid ClientUserId, IEnumerable<CartItemDto> Items, decimal Total, CartStatus Status);

// Favorites
public record ToggleFavoriteDto(Guid ProductId);