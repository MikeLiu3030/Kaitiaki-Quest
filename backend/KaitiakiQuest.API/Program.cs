using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Register DbContext (using SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Register Identity（using ApplicationUser adn IdentityRole）
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3. Register Controllers
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