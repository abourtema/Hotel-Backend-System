using System.Text;
using HotelResortBooking.Application.Abstractions.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelResortBooking.Infrastructure.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<GoogleAuthOptions>(configuration.GetSection(GoogleAuthOptions.SectionName));
        services.Configure<AppleAuthOptions>(configuration.GetSection(AppleAuthOptions.SectionName));

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.SecretKey) || jwt.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SecretKey must be configured and at least 32 characters.");
        }

        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        services.AddOidcJwksClient();
        services.AddSingleton(new GoogleOidcConfigurationProvider());
        services.AddSingleton(new AppleOidcConfigurationProvider());
        services.AddSingleton<IGoogleIdTokenValidator>(sp => new GoogleIdTokenValidator(
            sp.GetRequiredService<IOptions<GoogleAuthOptions>>(),
            sp.GetRequiredService<GoogleOidcConfigurationProvider>(),
            sp.GetRequiredService<ILogger<GoogleIdTokenValidator>>(),
            sp.GetRequiredService<IHttpClientFactory>()));
        services.AddSingleton<IAppleIdentityTokenValidator>(sp => new AppleIdentityTokenValidator(
            sp.GetRequiredService<IOptions<AppleAuthOptions>>(),
            sp.GetRequiredService<AppleOidcConfigurationProvider>(),
            sp.GetRequiredService<ILogger<AppleIdentityTokenValidator>>(),
            sp.GetRequiredService<IHttpClientFactory>()));

        var requireHttps = configuration.GetValue("Authentication:RequireHttpsMetadata", false);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = requireHttps;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                    NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            type = "https://httpstatuses.com/401",
                            title = "Unauthorized",
                            status = 401,
                            detail = "Authentication is required.",
                            traceId = context.HttpContext.TraceIdentifier
                        });
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/problem+json";
                        await context.Response.WriteAsJsonAsync(new
                        {
                            type = "https://httpstatuses.com/403",
                            title = "Forbidden",
                            status = 403,
                            detail = "You do not have permission to access this resource.",
                            traceId = context.HttpContext.TraceIdentifier
                        });
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
        });

        return services;
    }
}
