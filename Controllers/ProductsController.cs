using System.Security.Claims;
using EcommerceA.Data;
using EcommerceA.DTOs;
using EcommerceA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceA.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private Role CurrentRole => Enum.Parse<Role>(User.FindFirstValue(ClaimTypes.Role)!);

    // Públicos (clientes/visitantes)
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll([FromQuery] bool? onlyActive = true)
    {
        var q = db.Products.AsNoTracking();
        if (onlyActive == true) q = q.Where(p => p.IsActive);
        var list = await q
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock, p.IsActive, p.OwnerCompanyUserId))
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDto>> GetOne(Guid id)
    {
        var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock, p.IsActive, p.OwnerCompanyUserId);
    }

    // Empresa: crear producto propio
    [HttpPost]
    [Authorize(Roles = nameof(Role.Company))]
    public async Task<ActionResult<ProductDto>> Create(ProductCreateDto dto)
    {
        var product = new Product
        {
            OwnerCompanyUserId = CurrentUserId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            Stock = dto.Stock
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), new { id = product.Id },
            new ProductDto(product.Id, product.Name, product.Description, product.Price, product.Stock, product.IsActive, product.OwnerCompanyUserId));
    }

    // Empresa: update solo si es dueño
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = nameof(Role.Company))]
    public async Task<ActionResult<ProductDto>> Update(Guid id, ProductUpdateDto dto)
    {
        var p = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (p.OwnerCompanyUserId != CurrentUserId) return Forbid();

        if (dto.Name is not null) p.Name = dto.Name;
        if (dto.Description is not null) p.Description = dto.Description;
        if (dto.Price is not null) p.Price = dto.Price.Value;
        if (dto.Stock is not null) p.Stock = dto.Stock.Value;
        if (dto.IsActive is not null) p.IsActive = dto.IsActive.Value;
        p.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock, p.IsActive, p.OwnerCompanyUserId);
    }

    // Empresa: delete solo si es dueño
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(Role.Company))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await db.Products.FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        if (p.OwnerCompanyUserId != CurrentUserId) return Forbid();

        db.Products.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }
}