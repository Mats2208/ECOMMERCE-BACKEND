using System.Security.Claims;
using EcommerceA.Data;
using EcommerceA.DTOs;
using EcommerceA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceA.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize(Roles = nameof(Role.Client))]
public class FavoritesController(AppDbContext db) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("toggle")]
    public async Task<IActionResult> Toggle(ToggleFavoriteDto dto)
    {
        var user = await db.Users.FirstAsync(u => u.Id == CurrentUserId);
        var favs = user.GetFavoriteIds();
        if (!await db.Products.AnyAsync(p => p.Id == dto.ProductId && p.IsActive))
            return NotFound("Product not found or inactive.");

        if (!favs.Add(dto.ProductId))
            favs.Remove(dto.ProductId); // si ya estaba, lo quita

        user.SetFavoriteIds(favs);
        await db.SaveChangesAsync();
        return Ok(new { favorites = favs });
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == CurrentUserId);
        var favs = user.GetFavoriteIds();
        var products = await db.Products
            .Where(p => favs.Contains(p.Id) && p.IsActive)
            .Select(p => new { p.Id, p.Name, p.Price, p.Stock })
            .ToListAsync();

        return Ok(products);
    }
}