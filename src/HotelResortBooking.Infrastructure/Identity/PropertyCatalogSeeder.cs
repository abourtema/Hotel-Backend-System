using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Infrastructure.Configuration;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Identity;

public static class PropertyCatalogSeeder
{
    public static readonly Guid RadissonBluPropertyId = Guid.Parse("11111111-aaaa-4bbb-8ccc-111111111111");

    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private const int InventoryDays = 400;
    private const string DefaultWishlistName = "My Wishlist";

    public static IReadOnlyList<SeedPropertyDefinition> Definitions { get; } = BuildDefinitions();

    public static async Task SeedAsync(
        ApplicationDbContext db,
        StorageOptions storage,
        string contentRootPath,
        CancellationToken cancellationToken = default)
    {
        var amenities = await EnsureAmenitiesAsync(db, cancellationToken);

        var root = storage.GetAbsoluteRootPath(contentRootPath);
        Directory.CreateDirectory(root);

        if (!await IsCatalogCurrentAsync(db, cancellationToken))
        {
            await WipePropertyCatalogAsync(db, cancellationToken);
            var imageCache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            foreach (var definition in Definitions)
            {
                await SeedPropertyAsync(db, definition, amenities, root, imageCache, cancellationToken);
            }
        }

        await EnsureLocalImagesAsync(root, cancellationToken);
        await EnsureInventoryCoverageAsync(db, cancellationToken);
        await SyncPublicImageUrlsAsync(db, cancellationToken);
        await EnsureDemoWishlistsAsync(db, cancellationToken);
    }

    private static async Task<bool> IsCatalogCurrentAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var properties = await db.Properties
            .AsNoTracking()
            .Select(p => new { p.Id, p.PropertyType })
            .ToListAsync(cancellationToken);

        if (properties.Count != Definitions.Count)
        {
            return false;
        }

        var expected = Definitions.ToDictionary(d => d.Id, d => d.PropertyType);
        return properties.All(p => expected.TryGetValue(p.Id, out var type) && p.PropertyType == type)
               && expected.Keys.All(id => properties.Any(p => p.Id == id));
    }

    private static async Task WipePropertyCatalogAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        await db.Payments.ExecuteDeleteAsync(cancellationToken);
        await db.ReviewImages.ExecuteDeleteAsync(cancellationToken);
        await db.ReviewTagAssignments.ExecuteDeleteAsync(cancellationToken);
        await db.Reviews.ExecuteDeleteAsync(cancellationToken);

        await db.LoyaltyTransactions
            .Where(t => t.BookingId != null)
            .ExecuteDeleteAsync(cancellationToken);

        await db.BookingStatusHistories.ExecuteDeleteAsync(cancellationToken);
        await db.BookingGuests.ExecuteDeleteAsync(cancellationToken);
        await db.BookingUpgrades.ExecuteDeleteAsync(cancellationToken);
        await db.BookingRooms.ExecuteDeleteAsync(cancellationToken);
        await db.Bookings.ExecuteDeleteAsync(cancellationToken);

        await db.WishlistItems.ExecuteDeleteAsync(cancellationToken);
        await db.RoomInventories.ExecuteDeleteAsync(cancellationToken);
        await db.RoomTypeUpgradeOptions.ExecuteDeleteAsync(cancellationToken);
        await db.RoomTypeAmenities.ExecuteDeleteAsync(cancellationToken);
        await db.RoomImages.ExecuteDeleteAsync(cancellationToken);
        await db.RoomTypes.ExecuteDeleteAsync(cancellationToken);
        await db.PropertyAmenities.ExecuteDeleteAsync(cancellationToken);
        await db.PropertyImages.ExecuteDeleteAsync(cancellationToken);
        await db.UpgradeOptions.ExecuteDeleteAsync(cancellationToken);
        await db.Properties.ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task SeedPropertyAsync(
        ApplicationDbContext db,
        SeedPropertyDefinition definition,
        Dictionary<string, Amenity> amenities,
        string storageRoot,
        Dictionary<string, byte[]> imageCache,
        CancellationToken cancellationToken)
    {
        var propertyFolder = Path.Combine(storageRoot, "hotels", definition.Id.ToString("N"));
        Directory.CreateDirectory(propertyFolder);

        var property = new Property
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
            PropertyType = definition.PropertyType,
            StarRating = definition.StarRating,
            ReviewScore = definition.ReviewScore,
            ReviewCount = definition.ReviewCount,
            AddressLine = definition.AddressLine,
            City = definition.City,
            Country = definition.Country,
            PostalCode = definition.PostalCode,
            Latitude = definition.Latitude,
            Longitude = definition.Longitude,
            CheckInTime = definition.CheckInTime,
            CheckOutTime = definition.CheckOutTime,
            CancellationPolicy = definition.CancellationPolicy,
            DepositPolicy = definition.DepositPolicy,
            IsActive = true,
            CreatedAt = SeededAt,
            UpdatedAt = SeededAt
        };

        for (var i = 0; i < definition.Images.Count; i++)
        {
            var image = definition.Images[i];
            var relativePath = await SaveImageAsync(
                propertyFolder,
                definition.Id,
                image.FileName,
                image.SourceUrl,
                imageCache,
                cancellationToken);
            property.Images.Add(new PropertyImage
            {
                PropertyId = definition.Id,
                ImageUrl = relativePath,
                AltText = image.AltText,
                SortOrder = i,
                IsPrimary = i == 0,
                CreatedAt = SeededAt
            });
        }

        foreach (var amenityName in definition.AmenityNames)
        {
            if (amenities.TryGetValue(amenityName, out var amenity))
            {
                property.PropertyAmenities.Add(new PropertyAmenity
                {
                    PropertyId = definition.Id,
                    AmenityId = amenity.Id
                });
            }
        }

        var rooms = new List<RoomType>();
        foreach (var roomDef in definition.Rooms)
        {
            var room = new RoomType
            {
                Id = roomDef.Id,
                PropertyId = definition.Id,
                Name = roomDef.Name,
                Description = roomDef.Description,
                RoomSize = roomDef.RoomSize,
                MaxAdults = roomDef.MaxAdults,
                MaxChildren = roomDef.MaxChildren,
                MaxGuests = roomDef.MaxGuests,
                BedType = roomDef.BedType,
                BasePricePerNight = roomDef.PricePerNight,
                Currency = "USD",
                IsActive = true
            };

            var roomPhoto = await SaveImageAsync(
                propertyFolder,
                definition.Id,
                roomDef.ImageFileName,
                roomDef.ImageSourceUrl,
                imageCache,
                cancellationToken);
            room.Images.Add(new RoomImage
            {
                RoomTypeId = room.Id,
                ImageUrl = roomPhoto,
                AltText = roomDef.Name,
                SortOrder = 0,
                IsPrimary = true,
                CreatedAt = SeededAt
            });

            foreach (var amenityName in roomDef.AmenityNames)
            {
                if (amenities.TryGetValue(amenityName, out var amenity))
                {
                    room.RoomTypeAmenities.Add(new RoomTypeAmenity
                    {
                        RoomTypeId = room.Id,
                        AmenityId = amenity.Id
                    });
                }
            }

            rooms.Add(room);
            property.RoomTypes.Add(room);
        }

        var upgrades = definition.Upgrades.Select(upgradeDef => new UpgradeOption
        {
            PropertyId = definition.Id,
            Name = upgradeDef.Name,
            Description = upgradeDef.Description,
            UpgradeType = upgradeDef.UpgradeType,
            Price = upgradeDef.Price,
            Currency = "USD",
            IsMandatory = false,
            IsRecommended = upgradeDef.Recommended,
            IsActive = true
        }).ToList();

        foreach (var upgrade in upgrades)
        {
            property.UpgradeOptions.Add(upgrade);
        }

        db.Properties.Add(property);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var room in rooms)
        {
            var roomDef = definition.Rooms.First(r => r.Id == room.Id);
            foreach (var upgrade in upgrades)
            {
                var upgradeDef = definition.Upgrades.First(u => u.Name == upgrade.Name);
                if (!upgradeDef.AssignToAllRooms
                    && !(upgradeDef.PremiumOnly && roomDef.IncludePremiumUpgrades))
                {
                    continue;
                }

                db.RoomTypeUpgradeOptions.Add(new RoomTypeUpgradeOption
                {
                    RoomTypeId = room.Id,
                    UpgradeOptionId = upgrade.Id
                });
            }
        }

        foreach (var room in rooms)
        {
            var roomDef = definition.Rooms.First(r => r.Id == room.Id);
            var inventoryStart = InventoryWindowStart();
            for (var day = 0; day < InventoryDays; day++)
            {
                db.RoomInventories.Add(new RoomInventory
                {
                    RoomTypeId = room.Id,
                    Date = inventoryStart.AddDays(day),
                    TotalRooms = roomDef.Inventory,
                    AvailableRooms = roomDef.Inventory,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<SeedPropertyDefinition> BuildDefinitions()
    {
        var cancellation = "Free cancellation up to 24 hours before check-in. Later cancellations or no-shows are charged one night including tax.";
        var deposit = "A valid credit card is required at booking. Government-issued ID is required at check-in.";

        return
        [
            new SeedPropertyDefinition
            {
                Id = RadissonBluPropertyId,
                Name = "Radisson Blu Al Mahary Hotel, Tripoli",
                Description =
                    "Five-star waterfront hotel on Al Fatah Street in Tripoli’s diplomatic quarter, overlooking the Mediterranean. " +
                    "Guests are a short walk from Martyrs’ Square and the Old City, with dining, Orchidia Spa, an outdoor pool, and meeting rooms.",
                PropertyType = PropertyType.Hotel,
                StarRating = 5,
                ReviewScore = 4.00m,
                ReviewCount = 151,
                AddressLine = "Al Fatah Street, Corniche, PO Box 91781",
                City = "Tripoli",
                Country = "Libya",
                PostalCode = "91781",
                Latitude = 32.89556,
                Longitude = 13.20028,
                CheckInTime = new TimeOnly(14, 0),
                CheckOutTime = new TimeOnly(12, 0),
                CancellationPolicy = cancellation,
                DepositPolicy = deposit,
                AmenityNames = ["Free Wi-Fi", "Outdoor pool", "Orchidia Spa", "Fitness centre", "On-site dining", "Parking", "Concierge"],
                Images =
                [
                    Img("exterior.jpg", ImageSources.Exterior, "Hotel exterior on the Tripoli corniche"),
                    Img("lobby.jpg", ImageSources.Lobby, "Lobby and reception"),
                    Img("pool.jpg", ImageSources.Pool, "Outdoor pool with sea views")
                ],
                Rooms =
                [
                    Room(Guid.Parse("21111111-aaaa-4bbb-8ccc-111111111111"), "Superior Sea View",
                        "Sea-inspired superior room with a balcony overlooking the Mediterranean.",
                        32m, 2, 1, 3, "King or Twin", 180m, 180, ImageSources.SuperiorRoom, "room-superior.jpg",
                        ["Free Wi-Fi", "Air conditioning", "Minibar", "Sea view"]),
                    Room(Guid.Parse("31111111-aaaa-4bbb-8ccc-111111111111"), "Business Class",
                        "Larger business-class room with a work desk and sea or city view balcony.",
                        38m, 2, 1, 3, "King", 240m, 90, ImageSources.BusinessRoom, "room-business.jpg",
                        ["Free Wi-Fi", "Air conditioning", "Work desk", "Minibar"], includePremium: true),
                    Room(Guid.Parse("41111111-aaaa-4bbb-8ccc-111111111111"), "Junior Suite",
                        "Separate sitting area, marble bathroom, and a wide balcony over the harbour.",
                        52m, 2, 2, 4, "King", 360m, 50, ImageSources.JuniorSuite, "room-junior.jpg",
                        ["Free Wi-Fi", "Sitting area", "Sea view", "Bathtub"], includePremium: true)
                ],
                Upgrades = HotelUpgrades()
            },
            new SeedPropertyDefinition
            {
                Id = Guid.Parse("a1111111-0000-4000-8000-000000000002"),
                Name = "Corinthia Hotel Tripoli",
                Description =
                    "Landmark hotel in central Tripoli with sweeping views toward the Mediterranean, multiple restaurants, " +
                    "a business centre, and spacious rooms designed for international travellers and conferences.",
                PropertyType = PropertyType.Hotel,
                StarRating = 5,
                ReviewScore = 4.20m,
                ReviewCount = 208,
                AddressLine = "Souq Al Thulatha Al Gadim, Al Fatah Street",
                City = "Tripoli",
                Country = "Libya",
                PostalCode = "00218",
                Latitude = 32.89210,
                Longitude = 13.18040,
                CheckInTime = new TimeOnly(15, 0),
                CheckOutTime = new TimeOnly(12, 0),
                CancellationPolicy = cancellation,
                DepositPolicy = deposit,
                AmenityNames = ["Free Wi-Fi", "Fitness centre", "On-site dining", "Parking", "Meeting rooms", "Concierge", "Room service"],
                Images =
                [
                    Img("exterior.jpg", ImageSources.CorinthiaExterior, "Corinthia Hotel facade"),
                    Img("restaurant.jpg", ImageSources.FineDining, "Hotel restaurant")
                ],
                Rooms =
                [
                    Room(Guid.Parse("a2111111-0000-4000-8000-000000000002"), "Deluxe City View",
                        "Quiet deluxe room with a king bed, work desk, and city views.",
                        34m, 2, 1, 3, "King", 165m, 120, ImageSources.DeluxeRoom, "room-deluxe.jpg",
                        ["Free Wi-Fi", "Air conditioning", "Work desk"]),
                    Room(Guid.Parse("a3111111-0000-4000-8000-000000000002"), "Club Suite",
                        "Suite with a living area, club-lounge access, and panoramic city views.",
                        58m, 2, 2, 4, "King", 310m, 40, ImageSources.ClubSuite, "room-suite.jpg",
                        ["Free Wi-Fi", "Sitting area", "Minibar", "In-room safe"], includePremium: true)
                ],
                Upgrades = HotelUpgrades()
            },
            new SeedPropertyDefinition
            {
                Id = Guid.Parse("a1111111-0000-4000-8000-000000000011"),
                Name = "Tajoura Beach Resort",
                Description =
                    "Low-rise beach resort east of Tripoli with private beach access, family pools, and evening dining facing the sea. " +
                    "Designed for longer stays with bungalow-style rooms and kids’ activities in summer.",
                PropertyType = PropertyType.Resort,
                StarRating = 4,
                ReviewScore = 4.35m,
                ReviewCount = 96,
                AddressLine = "Coastal Road, Tajoura",
                City = "Tajoura",
                Country = "Libya",
                PostalCode = "00218",
                Latitude = 32.88190,
                Longitude = 13.35020,
                CheckInTime = new TimeOnly(14, 0),
                CheckOutTime = new TimeOnly(11, 0),
                CancellationPolicy = cancellation,
                DepositPolicy = deposit,
                AmenityNames = ["Free Wi-Fi", "Outdoor pool", "On-site dining", "Parking", "Airport shuttle"],
                Images =
                [
                    Img("exterior.jpg", ImageSources.ResortPool, "Beach resort pool"),
                    Img("beach.jpg", ImageSources.Beach, "Resort grounds")
                ],
                Rooms =
                [
                    Room(Guid.Parse("a2111111-0000-4000-8000-000000000011"), "Garden Bungalow",
                        "Ground-floor bungalow opening onto a shared garden path to the beach.",
                        40m, 2, 2, 4, "King or Twin", 140m, 60, ImageSources.ResortBungalow, "room-garden.jpg",
                        ["Free Wi-Fi", "Air conditioning", "Tea and coffee"]),
                    Room(Guid.Parse("a3111111-0000-4000-8000-000000000011"), "Seafront Suite",
                        "Family suite with a terrace facing the Mediterranean.",
                        62m, 3, 2, 5, "King", 220m, 24, ImageSources.SeafrontSuite, "room-seafront.jpg",
                        ["Free Wi-Fi", "Sea view", "Sitting area"], includePremium: true)
                ],
                Upgrades = ResortUpgrades()
            },
            new SeedPropertyDefinition
            {
                Id = Guid.Parse("a1111111-0000-4000-8000-000000000012"),
                Name = "Jebel Akhdar Mountain Resort",
                Description =
                    "Hillside resort in the Green Mountain region near Al Bayda, with hiking access, cool summer evenings, " +
                    "and stone-built lodges overlooking terraced farmland and pine forest.",
                PropertyType = PropertyType.Resort,
                StarRating = 4,
                ReviewScore = 4.50m,
                ReviewCount = 64,
                AddressLine = "Shahhat Road, Al Bayda Heights",
                City = "Al Bayda",
                Country = "Libya",
                PostalCode = "00218",
                Latitude = 32.76250,
                Longitude = 21.75500,
                CheckInTime = new TimeOnly(14, 0),
                CheckOutTime = new TimeOnly(11, 0),
                CancellationPolicy = cancellation,
                DepositPolicy = deposit,
                AmenityNames = ["Free Wi-Fi", "On-site dining", "Parking", "Fitness centre"],
                Images =
                [
                    Img("exterior.jpg", ImageSources.MountainLodge, "Mountain resort lodge"),
                    Img("dining.jpg", ImageSources.LodgeDining, "Resort restaurant")
                ],
                Rooms =
                [
                    Room(Guid.Parse("a2111111-0000-4000-8000-000000000012"), "Mountain Lodge",
                        "Stone lodge room with a fireplace nook and forest views.",
                        36m, 2, 1, 3, "Queen", 125m, 40, ImageSources.LodgeRoom, "room-lodge.jpg",
                        ["Free Wi-Fi", "Air conditioning", "Tea and coffee"]),
                    Room(Guid.Parse("a3111111-0000-4000-8000-000000000012"), "Family Chalet",
                        "Two-bedroom chalet with a small kitchenette and terrace.",
                        70m, 4, 2, 6, "King and Twin", 195m, 16, ImageSources.Chalet, "room-chalet.jpg",
                        ["Free Wi-Fi", "Sitting area", "Tea and coffee"], includePremium: true)
                ],
                Upgrades = ResortUpgrades()
            },
            new SeedPropertyDefinition
            {
                Id = Guid.Parse("a1111111-0000-4000-8000-000000000021"),
                Name = "Medina Residences Tripoli",
                Description =
                    "Serviced apartments beside the old city, with full kitchens, weekly housekeeping, and walkable access to souqs, " +
                    "cafés, and the corniche. Suited to longer business or family stays.",
                PropertyType = PropertyType.Apartment,
                StarRating = 4,
                ReviewScore = 4.15m,
                ReviewCount = 73,
                AddressLine = "Omar Al Mukhtar Street, Medina",
                City = "Tripoli",
                Country = "Libya",
                PostalCode = "00218",
                Latitude = 32.89980,
                Longitude = 13.18090,
                CheckInTime = new TimeOnly(15, 0),
                CheckOutTime = new TimeOnly(11, 0),
                CancellationPolicy = cancellation,
                DepositPolicy = deposit,
                AmenityNames = ["Free Wi-Fi", "Parking", "Air conditioning", "Non-smoking"],
                Images =
                [
                    Img("exterior.jpg", ImageSources.ApartmentExterior, "Apartment building courtyard"),
                    Img("living.jpg", ImageSources.ApartmentLiving, "Living room")
                ],
                Rooms =
                [
                    Room(Guid.Parse("a2111111-0000-4000-8000-000000000021"), "One-Bedroom Apartment",
                        "Open living room, kitchen, and one bedroom with a queen bed.",
                        48m, 2, 1, 3, "Queen", 95m, 18, ImageSources.OneBedApartment, "apt-1bed.jpg",
                        ["Free Wi-Fi", "Air conditioning", "Tea and coffee"]),
                    Room(Guid.Parse("a3111111-0000-4000-8000-000000000021"), "Two-Bedroom Apartment",
                        "Family apartment with two bedrooms, a dining table, and a washer.",
                        72m, 4, 2, 6, "Queen and Twin", 135m, 10, ImageSources.TwoBedApartment, "apt-2bed.jpg",
                        ["Free Wi-Fi", "Sitting area", "Work desk"], includePremium: true)
                ],
                Upgrades = ApartmentUpgrades()
            },
            new SeedPropertyDefinition
            {
                Id = Guid.Parse("a1111111-0000-4000-8000-000000000022"),
                Name = "Benghazi Harbor Suites",
                Description =
                    "Modern serviced apartments overlooking Benghazi harbour, with dedicated workspaces, fast Wi-Fi, " +
                    "and a 24-hour reception for business travellers.",
                PropertyType = PropertyType.Apartment,
                StarRating = 4,
                ReviewScore = 4.05m,
                ReviewCount = 41,
                AddressLine = "Al Corniche, Port District",
                City = "Benghazi",
                Country = "Libya",
                PostalCode = "00218",
                Latitude = 32.11650,
                Longitude = 20.06860,
                CheckInTime = new TimeOnly(15, 0),
                CheckOutTime = new TimeOnly(11, 0),
                CancellationPolicy = cancellation,
                DepositPolicy = deposit,
                AmenityNames = ["Free Wi-Fi", "Parking", "Fitness centre", "Concierge"],
                Images =
                [
                    Img("exterior.jpg", ImageSources.HarborExterior, "Harbor suites exterior"),
                    Img("suite.jpg", ImageSources.HarborInterior, "Harbor suite interior")
                ],
                Rooms =
                [
                    Room(Guid.Parse("a2111111-0000-4000-8000-000000000022"), "Studio Suite",
                        "Studio with a kitchenette, sofa bed, and harbour glimpses.",
                        38m, 2, 0, 2, "Queen", 85m, 22, ImageSources.StudioSuite, "apt-studio.jpg",
                        ["Free Wi-Fi", "Work desk", "Air conditioning"]),
                    Room(Guid.Parse("a3111111-0000-4000-8000-000000000022"), "Harbor One-Bedroom",
                        "One-bedroom suite with a separate lounge and harbour view.",
                        55m, 2, 1, 3, "King", 118m, 12, ImageSources.HarborBedroom, "apt-harbor.jpg",
                        ["Free Wi-Fi", "Sea view", "Sitting area"], includePremium: true)
                ],
                Upgrades = ApartmentUpgrades()
            },
            new SeedPropertyDefinition
            {
                Id = Guid.Parse("a1111111-0000-4000-8000-000000000031"),
                Name = "Villa Serene Janzour",
                Description =
                    "Private walled villa west of Tripoli with a garden, outdoor seating, and a cook’s kitchen. " +
                    "Rented as a whole home for families and small groups seeking privacy near the coast.",
                PropertyType = PropertyType.Villa,
                StarRating = 4,
                ReviewScore = 4.60m,
                ReviewCount = 29,
                AddressLine = "Janzour Coastal Road",
                City = "Janzour",
                Country = "Libya",
                PostalCode = "00218",
                Latitude = 32.81840,
                Longitude = 13.01080,
                CheckInTime = new TimeOnly(16, 0),
                CheckOutTime = new TimeOnly(10, 0),
                CancellationPolicy = cancellation,
                DepositPolicy = deposit,
                AmenityNames = ["Free Wi-Fi", "Parking", "Outdoor pool", "Air conditioning"],
                Images =
                [
                    Img("exterior.jpg", ImageSources.VillaPool, "Villa garden and pool"),
                    Img("living.jpg", ImageSources.VillaLiving, "Villa living room")
                ],
                Rooms =
                [
                    Room(Guid.Parse("a2111111-0000-4000-8000-000000000031"), "Garden Villa (entire home)",
                        "Three-bedroom villa with a pool, garden, and fully equipped kitchen.",
                        180m, 6, 3, 8, "King and Twin", 280m, 1, ImageSources.VillaInterior, "villa-garden.jpg",
                        ["Free Wi-Fi", "Outdoor pool", "Sitting area", "Air conditioning"], includePremium: true)
                ],
                Upgrades = VillaUpgrades()
            },
            new SeedPropertyDefinition
            {
                Id = Guid.Parse("a1111111-0000-4000-8000-000000000032"),
                Name = "Cyrene Garden Villa",
                Description =
                    "Restored villa near the Cyrene archaeological site, with a courtyard, olive trees, and guided-excursion arrangements. " +
                    "Ideal for travellers combining heritage visits with a private stay.",
                PropertyType = PropertyType.Villa,
                StarRating = 4,
                ReviewScore = 4.70m,
                ReviewCount = 18,
                AddressLine = "Shahat Village Road",
                City = "Shahat",
                Country = "Libya",
                PostalCode = "00218",
                Latitude = 32.82500,
                Longitude = 21.85800,
                CheckInTime = new TimeOnly(16, 0),
                CheckOutTime = new TimeOnly(10, 0),
                CancellationPolicy = cancellation,
                DepositPolicy = deposit,
                AmenityNames = ["Free Wi-Fi", "Parking", "On-site dining"],
                Images =
                [
                    Img("exterior.jpg", ImageSources.HeritageCourtyard, "Cyrene villa courtyard"),
                    Img("bedroom.jpg", ImageSources.HeritageBedroom, "Villa bedroom")
                ],
                Rooms =
                [
                    Room(Guid.Parse("a2111111-0000-4000-8000-000000000032"), "Heritage Villa (entire home)",
                        "Four-bedroom courtyard villa with dining hall and garden.",
                        210m, 8, 4, 10, "King and Twin", 320m, 1, ImageSources.HeritageVilla, "villa-heritage.jpg",
                        ["Free Wi-Fi", "Sitting area", "Tea and coffee"], includePremium: true)
                ],
                Upgrades = VillaUpgrades()
            },
            new SeedPropertyDefinition
            {
                Id = Guid.Parse("a1111111-0000-4000-8000-000000000041"),
                Name = "Old City Guest House Tripoli",
                Description =
                    "Small guest house in a restored Medina townhouse, with a rooftop tea terrace and rooms arranged around a central courtyard. " +
                    "A walkable base for exploring the old city on foot.",
                PropertyType = PropertyType.GuestHouse,
                StarRating = 3,
                ReviewScore = 4.40m,
                ReviewCount = 87,
                AddressLine = "Souq Al Mushir, Medina",
                City = "Tripoli",
                Country = "Libya",
                PostalCode = "00218",
                Latitude = 32.89690,
                Longitude = 13.17720,
                CheckInTime = new TimeOnly(13, 0),
                CheckOutTime = new TimeOnly(11, 0),
                CancellationPolicy = cancellation,
                DepositPolicy = deposit,
                AmenityNames = ["Free Wi-Fi", "On-site dining", "Non-smoking", "Concierge"],
                Images =
                [
                    Img("exterior.jpg", ImageSources.MedinaCourtyard, "Guest house courtyard"),
                    Img("room.jpg", ImageSources.GuestRoom, "Courtyard room")
                ],
                Rooms =
                [
                    Room(Guid.Parse("a2111111-0000-4000-8000-000000000041"), "Courtyard Double",
                        "Simple double room facing the inner courtyard, with shared terrace access.",
                        18m, 2, 0, 2, "Double", 55m, 8, ImageSources.GuestDouble, "gh-double.jpg",
                        ["Free Wi-Fi", "Air conditioning"]),
                    Room(Guid.Parse("a3111111-0000-4000-8000-000000000041"), "Rooftop Twin",
                        "Twin room on the roof level with Old City views.",
                        20m, 2, 1, 3, "Twin", 62m, 4, ImageSources.GuestTwin, "gh-twin.jpg",
                        ["Free Wi-Fi", "Tea and coffee"])
                ],
                Upgrades = GuestHouseUpgrades()
            },
            new SeedPropertyDefinition
            {
                Id = Guid.Parse("a1111111-0000-4000-8000-000000000042"),
                Name = "Ghadames Desert Guest House",
                Description =
                    "Family-run guest house in the oasis town of Ghadames, offering traditional rooms, home-cooked meals, " +
                    "and desert-guide introductions for overnight excursions.",
                PropertyType = PropertyType.GuestHouse,
                StarRating = 3,
                ReviewScore = 4.55m,
                ReviewCount = 52,
                AddressLine = "Old Town Lane, Ghadames Oasis",
                City = "Ghadames",
                Country = "Libya",
                PostalCode = "00218",
                Latitude = 30.13360,
                Longitude = 9.50070,
                CheckInTime = new TimeOnly(13, 0),
                CheckOutTime = new TimeOnly(10, 0),
                CancellationPolicy = cancellation,
                DepositPolicy = deposit,
                AmenityNames = ["Free Wi-Fi", "On-site dining", "Parking", "Airport shuttle"],
                Images =
                [
                    Img("exterior.jpg", ImageSources.DesertLodge, "Desert guest house"),
                    Img("dining.jpg", ImageSources.DesertDining, "Shared dining room")
                ],
                Rooms =
                [
                    Room(Guid.Parse("a2111111-0000-4000-8000-000000000042"), "Oasis Double",
                        "Traditional double room with thick walls and a small sitting alcove.",
                        22m, 2, 1, 3, "Double", 48m, 6, ImageSources.OasisRoom, "gh-oasis.jpg",
                        ["Free Wi-Fi", "Air conditioning"]),
                    Room(Guid.Parse("a3111111-0000-4000-8000-000000000042"), "Family Room",
                        "Larger room with space for two adults and two children.",
                        30m, 2, 2, 4, "Double and Twin", 68m, 3, ImageSources.FamilyRoom, "gh-family.jpg",
                        ["Free Wi-Fi", "Tea and coffee"])
                ],
                Upgrades = GuestHouseUpgrades()
            }
        ];
    }

    private static SeedImage Img(string fileName, string sourceUrl, string alt)
        => new() { FileName = fileName, SourceUrl = sourceUrl, AltText = alt };

    private static SeedRoomDefinition Room(
        Guid id,
        string name,
        string description,
        decimal size,
        int adults,
        int children,
        int guests,
        string bed,
        decimal price,
        int inventory,
        string imageUrl,
        string imageFile,
        string[] amenities,
        bool includePremium = false)
        => new()
        {
            Id = id,
            Name = name,
            Description = description,
            RoomSize = size,
            MaxAdults = adults,
            MaxChildren = children,
            MaxGuests = guests,
            BedType = bed,
            PricePerNight = price,
            Inventory = inventory,
            ImageSourceUrl = imageUrl,
            ImageFileName = imageFile,
            AmenityNames = amenities,
            IncludePremiumUpgrades = includePremium
        };

    private static SeedUpgradeDefinition[] HotelUpgrades() =>
    [
        new() { Name = "Buffet breakfast", Description = "Full breakfast buffet.", UpgradeType = UpgradeType.Breakfast, Price = 25m, Recommended = true, AssignToAllRooms = true },
        new() { Name = "Airport transfer", Description = "Private car, one way.", UpgradeType = UpgradeType.AirportTransfer, Price = 45m, Recommended = true, AssignToAllRooms = true },
        new() { Name = "Spa ritual", Description = "60-minute massage.", UpgradeType = UpgradeType.Spa, Price = 70m, PremiumOnly = true },
        new() { Name = "Late checkout", Description = "Guaranteed late checkout until 16:00.", UpgradeType = UpgradeType.PremiumService, Price = 40m, AssignToAllRooms = true }
    ];

    private static SeedUpgradeDefinition[] ResortUpgrades() =>
    [
        new() { Name = "Breakfast", Description = "Resort breakfast.", UpgradeType = UpgradeType.Breakfast, Price = 18m, Recommended = true, AssignToAllRooms = true },
        new() { Name = "Airport transfer", Description = "Private transfer, one way.", UpgradeType = UpgradeType.AirportTransfer, Price = 40m, AssignToAllRooms = true },
        new() { Name = "Extra bed", Description = "Rollaway bed.", UpgradeType = UpgradeType.ExtraBed, Price = 25m, AssignToAllRooms = true }
    ];

    private static SeedUpgradeDefinition[] ApartmentUpgrades() =>
    [
        new() { Name = "Welcome groceries", Description = "Starter groceries for the kitchen.", UpgradeType = UpgradeType.Other, Price = 20m, AssignToAllRooms = true },
        new() { Name = "Airport transfer", Description = "Private transfer, one way.", UpgradeType = UpgradeType.AirportTransfer, Price = 35m, AssignToAllRooms = true }
    ];

    private static SeedUpgradeDefinition[] VillaUpgrades() =>
    [
        new() { Name = "Private chef dinner", Description = "In-villa dinner for the group.", UpgradeType = UpgradeType.PremiumService, Price = 120m, Recommended = true, AssignToAllRooms = true },
        new() { Name = "Airport transfer", Description = "Private van, one way.", UpgradeType = UpgradeType.AirportTransfer, Price = 55m, AssignToAllRooms = true }
    ];

    private static SeedUpgradeDefinition[] GuestHouseUpgrades() =>
    [
        new() { Name = "Home breakfast", Description = "Home-cooked breakfast.", UpgradeType = UpgradeType.Breakfast, Price = 10m, Recommended = true, AssignToAllRooms = true },
        new() { Name = "Local guide half-day", Description = "Walking or oasis orientation.", UpgradeType = UpgradeType.Other, Price = 30m, AssignToAllRooms = true }
    ];

    private static DateOnly InventoryWindowStart()
        => DateOnly.FromDateTime(DateTime.UtcNow.Date);

    private static async Task EnsureInventoryCoverageAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var start = InventoryWindowStart();
        var endExclusive = start.AddDays(InventoryDays);
        var inventoryByRoomId = Definitions
            .SelectMany(d => d.Rooms)
            .ToDictionary(r => r.Id, r => r.Inventory);
        var roomIds = await db.RoomTypes.AsNoTracking().Select(rt => rt.Id).ToListAsync(cancellationToken);

        foreach (var roomId in roomIds)
        {
            var existingDates = (await db.RoomInventories
                    .AsNoTracking()
                    .Where(i => i.RoomTypeId == roomId && i.Date >= start && i.Date < endExclusive)
                    .Select(i => i.Date)
                    .ToListAsync(cancellationToken))
                .ToHashSet();
            var totalRooms = inventoryByRoomId.TryGetValue(roomId, out var seeded)
                ? Math.Max(1, seeded)
                : 1;
            var availableRooms = totalRooms;

            for (var day = 0; day < InventoryDays; day++)
            {
                var date = start.AddDays(day);
                if (existingDates.Contains(date))
                {
                    continue;
                }

                db.RoomInventories.Add(new RoomInventory
                {
                    RoomTypeId = roomId,
                    Date = date,
                    TotalRooms = totalRooms,
                    AvailableRooms = availableRooms,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SyncPublicImageUrlsAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var byId = Definitions.ToDictionary(d => d.Id);
        var images = await db.PropertyImages.ToListAsync(cancellationToken);
        foreach (var image in images)
        {
            if (!byId.TryGetValue(image.PropertyId, out var definition)
                || image.SortOrder < 0
                || image.SortOrder >= definition.Images.Count)
            {
                continue;
            }

            var fileName = definition.Images[image.SortOrder].FileName;
            if (IsSeededOrRemoteImageUrl(image.ImageUrl, fileName))
            {
                image.ImageUrl = LocalHotelImagePath(definition.Id, fileName);
            }
        }

        var roomImages = await db.RoomImages.ToListAsync(cancellationToken);
        var roomsById = Definitions
            .SelectMany(d => d.Rooms)
            .ToDictionary(r => r.Id);
        var propertyIdByRoomId = Definitions
            .SelectMany(d => d.Rooms.Select(r => (RoomId: r.Id, PropertyId: d.Id)))
            .ToDictionary(x => x.RoomId, x => x.PropertyId);
        foreach (var image in roomImages)
        {
            if (!roomsById.TryGetValue(image.RoomTypeId, out var roomDef)
                || !propertyIdByRoomId.TryGetValue(image.RoomTypeId, out var propertyId))
            {
                continue;
            }

            if (IsSeededOrRemoteImageUrl(image.ImageUrl, roomDef.ImageFileName))
            {
                image.ImageUrl = LocalHotelImagePath(propertyId, roomDef.ImageFileName);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDemoWishlistsAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var demoPropertyIds = Definitions
            .Where(d => d.City.Equals("Tripoli", StringComparison.OrdinalIgnoreCase))
            .Select(d => d.Id)
            .ToArray();
        if (demoPropertyIds.Length == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var userIds = await db.Users.AsNoTracking().Select(u => u.Id).ToListAsync(cancellationToken);
        foreach (var userId in userIds)
        {
            var wishlist = await db.Wishlists
                .Include(w => w.Items)
                .FirstOrDefaultAsync(
                    w => w.UserId == userId && w.Name == DefaultWishlistName,
                    cancellationToken);
            if (wishlist is null)
            {
                wishlist = new Wishlist
                {
                    UserId = userId,
                    Name = DefaultWishlistName,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Wishlists.Add(wishlist);
                await db.SaveChangesAsync(cancellationToken);
            }

            foreach (var propertyId in demoPropertyIds)
            {
                if (wishlist.Items.Any(i => i.PropertyId == propertyId))
                {
                    continue;
                }

                db.WishlistItems.Add(new WishlistItem
                {
                    WishlistId = wishlist.Id,
                    PropertyId = propertyId,
                    CreatedAt = now
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, Amenity>> EnsureAmenitiesAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var definitions = new (string Name, string Description, string Icon)[]
        {
            ("Free Wi-Fi", "High-speed wireless internet in rooms and public areas.", "wifi"),
            ("Outdoor pool", "Outdoor swimming pool.", "pool"),
            ("Orchidia Spa", "Spa and wellness treatments.", "spa"),
            ("Fitness centre", "Fitness centre.", "gym"),
            ("On-site dining", "Restaurant or shared dining.", "restaurant"),
            ("Room service", "In-room dining.", "room-service"),
            ("Parking", "On-site parking.", "parking"),
            ("Meeting rooms", "Meeting and event rooms.", "meeting"),
            ("Concierge", "Front desk and concierge.", "concierge"),
            ("Airport shuttle", "Airport transfer on request.", "shuttle"),
            ("Air conditioning", "Individually controlled air conditioning.", "ac"),
            ("Minibar", "In-room minibar.", "minibar"),
            ("In-room safe", "In-room safe.", "safe"),
            ("Tea and coffee", "Tea and coffee making facilities.", "coffee"),
            ("Sea view", "Sea or harbour views.", "sea"),
            ("Work desk", "Work desk.", "desk"),
            ("Sitting area", "Sitting or living area.", "sofa"),
            ("Bathtub", "Bathtub in the bathroom.", "bath"),
            ("Accessible rooms", "Accessible rooms on request.", "accessible"),
            ("Non-smoking", "Non-smoking rooms.", "no-smoking")
        };

        var existing = await db.Amenities.ToListAsync(cancellationToken);
        var map = existing.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var (name, description, icon) in definitions)
        {
            if (map.ContainsKey(name))
            {
                continue;
            }

            var amenity = new Amenity
            {
                Name = name,
                Description = description,
                Icon = icon,
                IsActive = true,
                CreatedAt = SeededAt,
                UpdatedAt = SeededAt
            };
            db.Amenities.Add(amenity);
            map[name] = amenity;
        }

        await db.SaveChangesAsync(cancellationToken);
        return map;
    }

    private static async Task<string> SaveImageAsync(
        string propertyFolder,
        Guid propertyId,
        string fileName,
        string sourceUrl,
        Dictionary<string, byte[]> imageCache,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(propertyFolder);
        var fullPath = Path.Combine(propertyFolder, fileName);

        if (!File.Exists(fullPath) || new FileInfo(fullPath).Length < 2048)
        {
            var bytes = await GetImageBytesAsync(sourceUrl, imageCache, cancellationToken);
            await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);
        }

        return LocalHotelImagePath(propertyId, fileName);
    }

    private static async Task EnsureLocalImagesAsync(string storageRoot, CancellationToken cancellationToken)
    {
        var imageCache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in Definitions)
        {
            var propertyFolder = Path.Combine(storageRoot, "hotels", definition.Id.ToString("N"));
            foreach (var image in definition.Images)
            {
                await SaveImageAsync(
                    propertyFolder,
                    definition.Id,
                    image.FileName,
                    image.SourceUrl,
                    imageCache,
                    cancellationToken);
            }

            foreach (var roomDef in definition.Rooms)
            {
                await SaveImageAsync(
                    propertyFolder,
                    definition.Id,
                    roomDef.ImageFileName,
                    roomDef.ImageSourceUrl,
                    imageCache,
                    cancellationToken);
            }
        }
    }

    private static async Task<byte[]> GetImageBytesAsync(
        string sourceUrl,
        Dictionary<string, byte[]> imageCache,
        CancellationToken cancellationToken)
    {
        if (imageCache.TryGetValue(sourceUrl, out var cached))
        {
            return cached;
        }

        try
        {
            using var response = await ImageHttp.GetAsync(sourceUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (bytes.Length < 2048)
            {
                throw new InvalidOperationException("Downloaded image is too small.");
            }

            imageCache[sourceUrl] = bytes;
            return bytes;
        }
        catch
        {
            imageCache[sourceUrl] = MinimalJpeg;
            return MinimalJpeg;
        }
    }

    private static string LocalHotelImagePath(Guid propertyId, string fileName)
        => Path.Combine("hotels", propertyId.ToString("N"), fileName).Replace('\\', '/');

    private static bool IsSeededOrRemoteImageUrl(string imageUrl, string fileName)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return true;
        }

        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalized = imageUrl.Replace('\\', '/');
        return normalized.EndsWith("/" + fileName, StringComparison.OrdinalIgnoreCase)
               || string.Equals(Path.GetFileName(normalized), fileName, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly HttpClient ImageHttp = CreateImageHttp();

    private static HttpClient CreateImageHttp()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("HotelResortBooking/1.0 (property-catalog-seeder)");
        client.DefaultRequestHeaders.Accept.ParseAdd("image/jpeg,image/webp,image/*,*/*;q=0.8");
        return client;
    }

    private static class ImageSources
    {
        public const string Exterior = "https://images.unsplash.com/photo-1566073771259-6a8506099945?auto=format&fit=crop&w=1600&q=80";
        public const string Lobby = "https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?auto=format&fit=crop&w=1600&q=80";
        public const string Pool = "https://images.unsplash.com/photo-1571896349842-33c89424de2d?auto=format&fit=crop&w=1600&q=80";
        public const string SuperiorRoom = "https://images.unsplash.com/photo-1611892440504-42a792e24d32?auto=format&fit=crop&w=1600&q=80";
        public const string BusinessRoom = "https://images.unsplash.com/photo-1590490360182-c33d57733427?auto=format&fit=crop&w=1600&q=80";
        public const string JuniorSuite = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=1600&q=80";
        public const string CorinthiaExterior = "https://images.unsplash.com/photo-1564501049412-61c2a3083791?auto=format&fit=crop&w=1600&q=80";
        public const string FineDining = "https://images.unsplash.com/photo-1414235077428-338989a2e8c0?auto=format&fit=crop&w=1600&q=80";
        public const string DeluxeRoom = "https://images.unsplash.com/photo-1631049307264-da0ec9d70304?auto=format&fit=crop&w=1600&q=80";
        public const string ClubSuite = "https://images.unsplash.com/photo-1578683010236-d716f9a3f461?auto=format&fit=crop&w=1600&q=80";
        public const string ResortPool = "https://images.unsplash.com/photo-1571003123894-1f0594d2b5d9?auto=format&fit=crop&w=1600&q=80";
        public const string Beach = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=1600&q=80";
        public const string ResortBungalow = "https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?auto=format&fit=crop&w=1600&q=80";
        public const string SeafrontSuite = "https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?auto=format&fit=crop&w=1600&q=80";
        public const string MountainLodge = "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?auto=format&fit=crop&w=1600&q=80";
        public const string LodgeDining = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&w=1600&q=80";
        public const string LodgeRoom = "https://images.unsplash.com/photo-1591088398332-8a7791972843?auto=format&fit=crop&w=1600&q=80";
        public const string Chalet = "https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?auto=format&fit=crop&w=1600&q=80";
        public const string ApartmentExterior = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1600&q=80";
        public const string ApartmentLiving = "https://images.unsplash.com/photo-1512918728675-ed5a9ecdebfd?auto=format&fit=crop&w=1600&q=80";
        public const string OneBedApartment = "https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?auto=format&fit=crop&w=1600&q=80";
        public const string TwoBedApartment = "https://images.unsplash.com/photo-1493809842364-78817add7ffb?auto=format&fit=crop&w=1600&q=80";
        public const string HarborExterior = "https://images.unsplash.com/photo-1445019980597-93fa8acb246c?auto=format&fit=crop&w=1600&q=80";
        public const string HarborInterior = "https://images.unsplash.com/photo-1566665797739-1674de7a421a?auto=format&fit=crop&w=1600&q=80";
        public const string StudioSuite = "https://images.unsplash.com/photo-1484101403633-562f891dc89a?auto=format&fit=crop&w=1600&q=80";
        public const string HarborBedroom = "https://images.unsplash.com/photo-1618773928121-c32242e63f39?auto=format&fit=crop&w=1600&q=80";
        public const string VillaPool = "https://images.unsplash.com/photo-1613490493576-7fde63acd811?auto=format&fit=crop&w=1600&q=80";
        public const string VillaLiving = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1600&q=80";
        public const string VillaInterior = "https://images.unsplash.com/photo-1600210492486-724fe5c67fb0?auto=format&fit=crop&w=1600&q=80";
        public const string HeritageCourtyard = "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?auto=format&fit=crop&w=1600&q=80";
        public const string HeritageBedroom = "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?auto=format&fit=crop&w=1600&q=80";
        public const string HeritageVilla = "https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?auto=format&fit=crop&w=1600&q=80";
        public const string MedinaCourtyard = "https://images.unsplash.com/photo-1480074568708-e7b720bb3f09?auto=format&fit=crop&w=1600&q=80";
        public const string GuestRoom = "https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=1600&q=80";
        public const string GuestDouble = "https://images.unsplash.com/photo-1560185007-cde436f6a4d0?auto=format&fit=crop&w=1600&q=80";
        public const string GuestTwin = "https://images.unsplash.com/photo-1554995207-c18c203602cb?auto=format&fit=crop&w=1600&q=80";
        public const string DesertLodge = "https://images.unsplash.com/photo-1509316785289-025f5b846b35?auto=format&fit=crop&w=1600&q=80";
        public const string DesertDining = "https://images.unsplash.com/photo-1559339352-11d035aa65de?auto=format&fit=crop&w=1600&q=80";
        public const string OasisRoom = "https://images.unsplash.com/photo-1518684079-3c830dcef090?auto=format&fit=crop&w=1600&q=80";
        public const string FamilyRoom = "https://images.unsplash.com/photo-1631049552057-403cdb8f0658?auto=format&fit=crop&w=1600&q=80";
    }

    private static readonly byte[] MinimalJpeg =
    [
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01,
        0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08,
        0x07, 0x07, 0x07, 0x09, 0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
        0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20, 0x24, 0x2E, 0x27, 0x20,
        0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29, 0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27,
        0x39, 0x3D, 0x38, 0x32, 0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
        0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0xFF, 0xC4, 0x00, 0x14,
        0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0x7F, 0xFF, 0xD9
    ];
}

public sealed class SeedPropertyDefinition
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required PropertyType PropertyType { get; init; }
    public required int StarRating { get; init; }
    public required decimal ReviewScore { get; init; }
    public required int ReviewCount { get; init; }
    public required string AddressLine { get; init; }
    public required string City { get; init; }
    public required string Country { get; init; }
    public string? PostalCode { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required TimeOnly CheckInTime { get; init; }
    public required TimeOnly CheckOutTime { get; init; }
    public string? CancellationPolicy { get; init; }
    public string? DepositPolicy { get; init; }
    public required IReadOnlyList<string> AmenityNames { get; init; }
    public required IReadOnlyList<SeedImage> Images { get; init; }
    public required IReadOnlyList<SeedRoomDefinition> Rooms { get; init; }
    public required IReadOnlyList<SeedUpgradeDefinition> Upgrades { get; init; }
}

public sealed class SeedImage
{
    public required string FileName { get; init; }
    public required string SourceUrl { get; init; }
    public required string AltText { get; init; }
}

public sealed class SeedRoomDefinition
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal RoomSize { get; init; }
    public required int MaxAdults { get; init; }
    public required int MaxChildren { get; init; }
    public required int MaxGuests { get; init; }
    public required string BedType { get; init; }
    public required decimal PricePerNight { get; init; }
    public required int Inventory { get; init; }
    public required string ImageSourceUrl { get; init; }
    public required string ImageFileName { get; init; }
    public required IReadOnlyList<string> AmenityNames { get; init; }
    public bool IncludePremiumUpgrades { get; init; }
}

public sealed class SeedUpgradeDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required UpgradeType UpgradeType { get; init; }
    public required decimal Price { get; init; }
    public bool Recommended { get; init; }
    public bool AssignToAllRooms { get; init; }
    public bool PremiumOnly { get; init; }
}
