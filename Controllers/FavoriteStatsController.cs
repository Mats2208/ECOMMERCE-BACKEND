using System.Security.Claims;
using EcommerceA.Data;
using EcommerceA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceA.Controllers;

[ApiController]
[Route("api/favorites/stats")]
public class FavoritesStatsController(AppDbContext db) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

    // 1) Conteo por producto (público)
    [HttpGet("product/{productId:guid}")]
    public async Task<IActionResult> ProductCount(Guid productId)
    {
        var count = await db.Favorites.CountAsync(f => f.ProductId == productId);
        return Ok(new { productId, favorites = count });
    }

    // 2) Top productos por likes (público/para vitrina)
    [HttpGet("top")]
    public async Task<IActionResult> Top([FromQuery] int take = 10, [FromQuery] bool onlyActive = true)
    {
        var q = from f in db.Favorites
                join p in db.Products on f.ProductId equals p.Id
                where !onlyActive || p.IsActive
                group f by new { p.Id, p.Name, p.OwnerCompanyUserId } into g
                orderby g.Count() descending
                select new {
                    productId = g.Key.Id,
                    name = g.Key.Name,
                    ownerCompanyUserId = g.Key.OwnerCompanyUserId,
                    favorites = g.Count()
                };

        var data = await q.Take(Math.Clamp(take, 1, 100)).ToListAsync();
        return Ok(data);
    }

    // 3) Para empresas: mis productos con conteo de likes (requiere rol Company)
    [HttpGet("mine")]
    [Authorize(Roles = nameof(Role.Company))]
    public async Task<IActionResult> Mine()
    {
        var companyId = CurrentUserId;
        var data = await (from p in db.Products
                          where p.OwnerCompanyUserId == companyId
                          join f in db.Favorites on p.Id equals f.ProductId into gj
                          from sub in gj.DefaultIfEmpty()
                          group sub by new { p.Id, p.Name, p.IsActive, p.Stock, p.Price } into g
                          select new {
                              productId = g.Key.Id,
                              name = g.Key.Name,
                              isActive = g.Key.IsActive,
                              stock = g.Key.Stock,
                              price = g.Key.Price,
                              favorites = g.Count(x => x != null)
                          })
                          .OrderByDescending(x => x.favorites)
                          .ToListAsync();

        return Ok(data);
    }

    // 4) Para empresas: conteos de un tercero (admin/partner)
    [HttpGet("company/{companyUserId:guid}")]
    [Authorize(Roles = $"{nameof(Role.AdminRoot)},{nameof(Role.Company)}")]
    public async Task<IActionResult> ByCompany(Guid companyUserId)
    {
        var data = await (from p in db.Products
                          where p.OwnerCompanyUserId == companyUserId
                          join f in db.Favorites on p.Id equals f.ProductId into gj
                          from sub in gj.DefaultIfEmpty()
                          group sub by new { p.Id, p.Name } into g
                          select new {
                              productId = g.Key.Id,
                              name = g.Key.Name,
                              favorites = g.Count(x => x != null)
                          })
                          .ToListAsync();

        return Ok(new { companyUserId, products = data });
    }
}
