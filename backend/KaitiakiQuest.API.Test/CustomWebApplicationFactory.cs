using KaitiakiQuest.API.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KaitiakiQuest.API.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        // Each Factory instance has an independent database name
        private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove the DbContext registered for the main project
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Replace it with an independent InMemory database
                services.AddDbContext<ApplicationDbContext>(options =>
                    options
                        .UseInMemoryDatabase(_dbName)
                        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
        }
    }
}
