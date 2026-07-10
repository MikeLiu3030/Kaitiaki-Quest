using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Implementations;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Register DbContext (using SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Identity（using ApplicationUser adn IdentityRole）
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register Services
builder.Services.AddScoped<IEcoMissionService, EcoMissionService>();
builder.Services.AddScoped<IUserMissionService, UserMissionService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();

// Register Controllers
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyForDevelopmentOnly!"))
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}


app.UseHttpsRedirection();
app.UseAuthentication(); 
app.UseAuthorization(); 

app.MapControllers();

// Add seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedData.InitializeAsync(services, userManager, roleManager);
        Console.WriteLine("✅ Database seeded successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Seeding error: {ex.Message}");
        throw;
    }
}

app.Run();