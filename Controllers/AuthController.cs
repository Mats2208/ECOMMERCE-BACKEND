using System.Security.Cryptography;
using EcommerceA.Data;
using EcommerceA.DTOs;
using EcommerceA.Models;
using EcommerceA.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcommerceA.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous] // 🔓 Abierto para que cualquiera pueda registrarse como Cliente
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        // ¿Está autenticado y es AdminRoot?
        var isAdmin = User?.Identity?.IsAuthenticated == true &&
                      User.IsInRole(nameof(Role.AdminRoot));

        // ¿El solicitante quiere crear Empresa o AdminRoot?
        var wantsElevated = dto.Role == Role.Company || dto.Role == Role.AdminRoot;

        // Si pide rol elevado sin JWT de AdminRoot -> 403
        if (wantsElevated && !isAdmin)
            return Forbid();

        // Si NO es admin, fuerza rol Cliente y limpia companyName
        var roleToCreate = isAdmin ? dto.Role : Role.Client;
        var companyName  = roleToCreate == Role.Company ? dto.CompanyName : null;

        if (await db.Users.AnyAsync(u => u.Email == dto.Email))
            return Conflict("Email already exists.");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = roleToCreate,
            CompanyName = companyName
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = tokens.GenerateToken(user);
        return Ok(new AuthResponseDto(token, user.Id, user.Role, user.Role == Role.AdminRoot));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null) return Unauthorized();

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized();

        var token = tokens.GenerateToken(user);
        return Ok(new AuthResponseDto(token, user.Id, user.Role, user.Role == Role.AdminRoot));
    }
}
