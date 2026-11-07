using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Infrastructure.Services;

namespace HotelResortBooking.Api.Middleware;

/// <summary>
/// Populates the scoped AccessContext with JWT identity or guest session info.
/// </summary>
public sealed class AccessContextMiddleware
{
    private readonly RequestDelegate _next;

    public AccessContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AccessContext accessContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdValue, out var userId))
            {
                accessContext.UserId = userId;
                accessContext.Mode = AccessMode.Authenticated;
                accessContext.Email = context.User.FindFirst(ClaimTypes.Email)?.Value
                    ?? context.User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
                accessContext.Roles = context.User.FindAll(ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToArray();
            }
        }
        else if (context.Request.Headers.TryGetValue("X-Guest-Session-Id", out var guestHeader)
                 && !string.IsNullOrWhiteSpace(guestHeader))
        {
            accessContext.Mode = AccessMode.Guest;
        }

        accessContext.IpAddress = GetClientIp(context);
        accessContext.UserAgent = context.Request.Headers.UserAgent.ToString();

        await _next(context);
    }

    private static string? GetClientIp(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded)
            && !string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
