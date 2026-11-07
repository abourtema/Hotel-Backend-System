using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelResortBooking.Api.Contracts.Auth;
using HotelResortBooking.Application.Security;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotelResortBooking.IntegrationTests;

public sealed class ManagementPortalApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ManagementPortalApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SeededManagementAdmin_CanLoginAndAccessDashboard()
    {
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "admin@hotelresortbooking.local",
            Password = "Admin123!"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.Equal("SuperAdmin", session!.Role);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        var dashboard = await client.GetAsync("/api/v1/management/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);

        var filtered = await client.GetAsync("/api/v1/management/dashboard?dateFrom=2026-09-07&dateTo=2026-09-07");
        Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);

        var payments = await client.GetAsync("/api/v1/management/payments?dateFrom=2026-09-07&dateTo=2026-09-07");
        Assert.Equal(HttpStatusCode.OK, payments.StatusCode);

        var reports = await client.GetAsync("/api/v1/management/reports/bookings?dateFrom=2026-09-07&dateTo=2026-09-07");
        Assert.Equal(HttpStatusCode.OK, reports.StatusCode);
    }

    [Fact]
    public async Task Dashboard_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/management/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_CustomerToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var email = $"cust-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password1!",
            FirstName = "Ada",
            LastName = "Lovelace"
        });
        var session = await register.Content.ReadFromJsonAsync<AuthenticationResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        var response = await client.GetAsync("/api/v1/management/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_SuperAdmin_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var email = $"admin-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password1!",
            FirstName = "Ada",
            LastName = "Lovelace"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = db.Users.Single(u => u.Email == email);
            user.Role = Roles.SuperAdmin;
            var superAdminRole = db.Roles.Single(r => r.Name == Roles.SuperAdmin);
            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = superAdminRole.Id,
                AssignedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Password1!"
        });
        var session = await login.Content.ReadFromJsonAsync<AuthenticationResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        var dashboard = await client.GetAsync("/api/v1/management/dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);

        var hotels = await client.GetAsync("/api/v1/management/hotels?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, hotels.StatusCode);

        var reports = await client.GetAsync("/api/v1/management/reports/bookings");
        Assert.Equal(HttpStatusCode.OK, reports.StatusCode);
    }

    [Fact]
    public async Task HotelManager_CannotRefundPayments()
    {
        var client = _factory.CreateClient();
        var email = $"hm-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password1!",
            FirstName = "Pat",
            LastName = "Lee"
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = db.Users.Single(u => u.Email == email);
            user.Role = Roles.HotelManager;
            var role = db.Roles.Single(r => r.Name == Roles.HotelManager);
            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Password1!"
        });
        var session = await login.Content.ReadFromJsonAsync<AuthenticationResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        var hotels = await client.GetAsync("/api/v1/management/hotels");
        Assert.Equal(HttpStatusCode.OK, hotels.StatusCode);

        var refund = await client.PostAsync($"/api/v1/management/payments/{Guid.NewGuid()}/refund", null);
        Assert.Equal(HttpStatusCode.Forbidden, refund.StatusCode);
    }
}
