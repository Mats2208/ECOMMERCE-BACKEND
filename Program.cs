using System.Text;
using EcommerceA.Data;
using EcommerceA.Models;
using EcommerceA.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Servicios
builder.Services.AddScoped<TokenService>();

// Auth JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization(); // necesario para [Authorize(Roles=..)]

builder.Services.AddControllers();

// Swagger + Bearer
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EcommerceA API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce: Bearer {tu_jwt}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// --- Seed AdminRoot (admin@admin.com / admin123) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Aplica migraciones existentes (no crea nuevas)
    db.Database.Migrate();

    var rootEmail = builder.Configuration["Root:Email"] ?? "admin@admin.com";
    var rootPass  = builder.Configuration["Root:Password"] ?? "admin123";

    var root = await db.Users.FirstOrDefaultAsync(u => u.Email == rootEmail);
    if (root is null)
    {
        root = new User
        {
            Email = rootEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(rootPass),
            Role = Role.AdminRoot,
            CompanyName = null
        };
        db.Users.Add(root);
        await db.SaveChangesAsync();
        Console.WriteLine($"[SEED] AdminRoot creado: {rootEmail}");
    }
    else if (root.Role != Role.AdminRoot)
    {
        root.Role = Role.AdminRoot;
        await db.SaveChangesAsync();
        Console.WriteLine($"[SEED] Usuario {rootEmail} promovido a AdminRoot.");
    }
}
// --- fin seed ---

// Pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication(); // << necesario antes de Authorization
app.UseAuthorization();

app.MapControllers();

app.Run();
