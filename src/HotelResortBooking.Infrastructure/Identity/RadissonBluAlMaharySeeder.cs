using HotelResortBooking.Infrastructure.Configuration;
using HotelResortBooking.Infrastructure.Persistence;

namespace HotelResortBooking.Infrastructure.Identity;

public static class RadissonBluAlMaharySeeder
{
    public static readonly Guid PropertyId = PropertyCatalogSeeder.RadissonBluPropertyId;

    public static Task SeedAsync(
        ApplicationDbContext db,
        StorageOptions storage,
        string contentRootPath,
        CancellationToken cancellationToken = default)
        => PropertyCatalogSeeder.SeedAsync(db, storage, contentRootPath, cancellationToken);
}
