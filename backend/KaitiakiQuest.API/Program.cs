using Azure.Core;
using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.Hubs;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Implementations;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;
using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("KaitiakiQuest.API.Test")]

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

// Register DbContext (using SQL Server)
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register DbContext (using SQL Server or InMemory based on environment)
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseInMemoryDatabase("TestDb_{Guid.NewGuid()}"));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// Register Identity（using ApplicationUser adn IdentityRole）
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register Services
builder.Services.AddScoped<IEcoMissionService, EcoMissionService>();
builder.Services.AddScoped<IUserMissionService, UserMissionService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IBadgeService, BadgeService>();

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
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "IloveYouForeverUntillTheSunDisapper!"))
    };

    // Read the Token from the Authorization Header
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };

    // Read the Tokken from the URL for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Get token from query string of URL.
            var accessToken = context.Request.Query["access_token"];

            // get request path
            var path = context.HttpContext.Request.Path;

            // if there is a Token in URL and the request is sending Hub route
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/teamHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;

        }
    };

});

// Register SignalR
builder.Services.AddSignalR();

// Register memory cache
builder.Services.AddMemoryCache();

// Add CORS service
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("AllowSpecificOrigin");
app.UseHttpsRedirection();
app.UseAuthentication(); 
app.UseAuthorization(); 

app.MapControllers();
app.MapHub<TeamHub>("/teamHub"); // Add the SignalR Hub mapping

// Add seed data
if (!builder.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            await SeedData.InitializeAsync(services, userManager, roleManager);
            Console.WriteLine("Database seeded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Seeding error: {ex.Message}");
            throw;
        }
    }
}


app.Run();