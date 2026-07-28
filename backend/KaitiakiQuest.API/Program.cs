using KaitiakiQuest.API.Data;
using KaitiakiQuest.API.Hubs;
using KaitiakiQuest.API.Models;
using KaitiakiQuest.API.Services.Implementations;
using KaitiakiQuest.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connectiong string 'DefaultConnection' not found");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
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
string jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("JWT Key is not configured.");
string jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT Issuer is not configured.");
string jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT Audience is not configured.");

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? throw new InvalidOperationException("CORS configuration is missing or empty. Please configure 'Cors:AllowedOrigins' in appsettings.json.");
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});



var app = builder.Build();

// Configure the HTTP request pipeline.

app.MapOpenApi();
app.MapScalarApiReference();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigin");
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
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }
}
app.Run();