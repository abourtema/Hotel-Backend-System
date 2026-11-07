using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotelResortBooking.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"hotel-tests-{Guid.NewGuid():N}";

    public CustomWebApplicationFactory()
    {
        // Environment variables override appsettings and are read when the host starts.
        // These values exist only for the in-memory integration host.
        Environment.SetEnvironmentVariable("Database__ConnectionString", "Host=localhost;Port=5432;Database=hotel_resort_booking_tests;Username=test;Password=test");
        Environment.SetEnvironmentVariable("Database__EnableSensitiveDataLogging", "false");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "HotelResortBooking.Api");
        Environment.SetEnvironmentVariable("Jwt__Audience", "HotelResortBooking.Api");
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "integration-test-signing-key-32-characters");
        Environment.SetEnvironmentVariable("ManagementSeed__Enabled", "true");
        Environment.SetEnvironmentVariable("ManagementSeed__Email", "admin@hotelresortbooking.local");
        Environment.SetEnvironmentVariable("ManagementSeed__Password", "Admin123!");
        Environment.SetEnvironmentVariable("ManagementSeed__FirstName", "System");
        Environment.SetEnvironmentVariable("ManagementSeed__LastName", "Admin");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureTestServices(services =>
        {
            RemoveDbContext<ApplicationDbContext>(services);
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    private static void RemoveDbContext<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        var toRemove = services.Where(descriptor =>
                descriptor.ServiceType == typeof(TContext) ||
                descriptor.ServiceType == typeof(DbContextOptions) ||
                descriptor.ServiceType == typeof(DbContextOptions<TContext>) ||
                descriptor.ServiceType == typeof(IDbContextOptionsConfiguration<TContext>) ||
                (descriptor.ServiceType.IsGenericType &&
                 descriptor.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                descriptor.ImplementationType == typeof(TContext))
            .ToList();

        foreach (var descriptor in toRemove)
        {
            services.Remove(descriptor);
        }
    }
}
