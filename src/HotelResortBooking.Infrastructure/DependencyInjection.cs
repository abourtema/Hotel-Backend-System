using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Infrastructure.Configuration;
using HotelResortBooking.Infrastructure.Identity;
using HotelResortBooking.Infrastructure.Persistence;
using HotelResortBooking.Infrastructure.Repositories;
using HotelResortBooking.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelResortBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AuthenticationOptions>(configuration.GetSection(AuthenticationOptions.SectionName));
        services.Configure<ManagementSeedOptions>(configuration.GetSection(ManagementSeedOptions.SectionName));
        services.Configure<ExternalServicesOptions>(configuration.GetSection(ExternalServicesOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        services.AddDbContext<ApplicationDbContext>((_, options) =>
        {
            var databaseOptions = configuration
                .GetSection(DatabaseOptions.SectionName)
                .Get<DatabaseOptions>() ?? new DatabaseOptions();

            options.UseNpgsql(databaseOptions.ConnectionString);

            if (databaseOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<AccessContext>();
        services.AddScoped<IAccessContext>(sp => sp.GetRequiredService<AccessContext>());
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<AccessContext>());
        services.AddScoped<IAuditContext>(sp => sp.GetRequiredService<AccessContext>());
        services.AddScoped<IAuditLogger, AuditLogger>();

        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IPaymentGateway, StubPaymentGateway>();
        services.AddSingleton<IImageStorage, LocalImageStorage>();
        services.AddSingleton<IImageUrlResolver, ImageUrlResolver>();

        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
        services.AddScoped<IRoomInventoryRepository, RoomInventoryRepository>();
        services.AddScoped<IUpgradeRepository, UpgradeRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IExternalLoginRepository, ExternalLoginRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IManagementUserRepository, ManagementUserRepository>();
        services.AddScoped<IManagementCatalogRepository, ManagementCatalogRepository>();
        services.AddScoped<IManagementQueryRepository, ManagementQueryRepository>();

        services.AddAuthenticationInfrastructure(configuration);

        return services;
    }
}
