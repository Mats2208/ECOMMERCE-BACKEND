using System.Security.Claims;
using EcommerceA.Data;
using EcommerceA.DTOs;
using EcommerceA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceA.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize(Roles = nameof(Role.Client))]
public class CartController(AppDbContext db) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Cart> GetOrCreateActiveCart(Guid clientId)
    {
        var cart = await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.ClientUserId == clientId && c.Status == CartStatus.Active);

        if (cart is null)
        {
            cart = new Cart { ClientUserId = clientId };
            db.Carts.Add(cart);
            await db.SaveChangesAsync();
        }

        return cart;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetMyCart()
    {
        var cart = await GetOrCreateActiveCart(CurrentUserId);
        var dto = new CartDto(
            cart.Id,
            cart.ClientUserId,
            cart.Items.Select(i => new CartItemDto(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)),
            cart.Total,
            cart.Status);
        return Ok(dto);
    }

    // Agregar al carrito (RESERVA stock)
    [HttpPost("add")]
    public async Task<ActionResult<CartDto>> Add(AddToCartDto dto)
    {
        var cart = await GetOrCreateActiveCart(CurrentUserId);
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId && p.IsActive);
        if (product is null) return NotFound("Product not found or inactive.");
        if (dto.Quantity <= 0) return BadRequest("Quantity must be > 0.");
        if (product.Stock < dto.Quantity) return BadRequest("Insufficient stock.");

        // reserva
        product.Stock -= dto.Quantity;

        var item = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);
        if (item is null)
        {
            cart.Items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = dto.Quantity
            });
        }
        else
        {
            item.Quantity += dto.Quantity;
        }

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await GetMyCart();
    }

    // Quitar del carrito (DEVUELVE stock)
    [HttpPost("remove")]
    public async Task<ActionResult<CartDto>> Remove(RemoveFromCartDto dto)
    {
        var cart = await GetOrCreateActiveCart(CurrentUserId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);
        if (item is null) return NotFound("Item not in cart.");
        if (dto.Quantity <= 0) return BadRequest("Quantity must be > 0.");

        var q = Math.Min(dto.Quantity, item.Quantity);
        item.Quantity -= q;

        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);
        if (product is not null) product.Stock += q; // devolver stock

        if (item.Quantity == 0) cart.Items.Remove(item);

        cart.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await GetMyCart();
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var cart = await GetOrCreateActiveCart(CurrentUserId);
        if (cart.Items.Count == 0) return BadRequest("Cart is empty.");

        cart.Status = CartStatus.CheckedOut;
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Aquí crearías la entidad Pedido/Order (futuro)
        return Ok(new { cart.Id, cart.Total, Status = cart.Status.ToString() });
    }

    [HttpPost("clear")]
    public async Task<ActionResult<CartDto>> Clear()
    {
        var cart = await GetOrCreateActiveCart(CurrentUserId);

        // devolver stock de todo
        var productIds = cart.Items.Select(i => i.ProductId).ToList();
        var products = await db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
        foreach (var i in cart.Items)
        {
            var p = products.FirstOrDefault(x => x.Id == i.ProductId);
            if (p is not null) p.Stock += i.Quantity;
        }

        cart.Items.Clear();
        cart.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return await GetMyCart();
    }
}