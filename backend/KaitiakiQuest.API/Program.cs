using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.Models;
using Scalar.AspNetCore;
using KaitiakiQuest.API.Services.Interfaces;
using KaitiakiQuest.API.Services.Implementations;

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

app.Run();