using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Infrastructure.Configuration;
using HotelResortBooking.Infrastructure.Identity;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace HotelResortBooking.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication ApplyPendingMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (db.Database.IsRelational())
        {
            db.Database.Migrate();
        }

        RolePermissionSeeder.SeedAsync(db).GetAwaiter().GetResult();

        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<ManagementSeedOptions>>().Value;
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        ManagementAdminSeeder.SeedAsync(db, passwordHasher, seedOptions).GetAwaiter().GetResult();

        var storageOptions = scope.ServiceProvider.GetRequiredService<IOptions<StorageOptions>>().Value;
        PropertyCatalogSeeder.SeedAsync(db, storageOptions, app.Environment.ContentRootPath)
            .GetAwaiter()
            .GetResult();

        return app;
    }

    public static WebApplication UseLocalImageFiles(this WebApplication app)
    {
        var storageOptions = app.Configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>()
            ?? new StorageOptions();
        var fileStorage = app.Configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>()
            ?? new FileStorageOptions();

        var rootPath = !string.IsNullOrWhiteSpace(fileStorage.LocalPath)
            ? Path.GetFullPath(fileStorage.LocalPath)
            : storageOptions.GetAbsoluteRootPath(app.Environment.ContentRootPath);
        Directory.CreateDirectory(rootPath);

        var requestPath = string.IsNullOrWhiteSpace(storageOptions.RequestPath)
            ? "/uploads"
            : storageOptions.RequestPath;
        if (!requestPath.StartsWith('/'))
        {
            requestPath = "/" + requestPath;
        }

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(rootPath),
            RequestPath = requestPath.TrimEnd('/')
        });

        return app;
    }

    public static WebApplication UseOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "Hotel Resort Booking API v1");
            options.DocumentTitle = "Hotel Resort Booking API";
            options.EnablePersistAuthorization();
        });
        return app;
    }
}
