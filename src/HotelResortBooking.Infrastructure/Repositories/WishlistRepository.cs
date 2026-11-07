using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class WishlistRepository : IWishlistRepository
{
    private const string DefaultWishlistName = "My Wishlist";

    private readonly ApplicationDbContext _dbContext;
    private readonly IImageUrlResolver _urls;

    public WishlistRepository(ApplicationDbContext dbContext, IImageUrlResolver urls)
    {
        _dbContext = dbContext;
        _urls = urls;
    }

    public async Task<WishlistData> GetOrCreateDefaultAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var wishlist = await _dbContext.Wishlists
            .Include(w => w.Items)
            .ThenInclude(i => i.Property)
            .ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(
                w => w.UserId == userId && w.Name == DefaultWishlistName,
                cancellationToken);

        if (wishlist is null)
        {
            wishlist = new Wishlist
            {
                UserId = userId,
                Name = DefaultWishlistName
            };
            _dbContext.Wishlists.Add(wishlist);
        }

        return Map(wishlist);
    }

    public Task<bool> ContainsPropertyAsync(
        Guid wishlistId,
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WishlistItems.AsNoTracking()
            .AnyAsync(i => i.WishlistId == wishlistId && i.PropertyId == propertyId, cancellationToken);
    }

    public async Task<WishlistItemData> AddItemAsync(
        Guid wishlistId,
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == propertyId, cancellationToken)
            ?? throw new InvalidOperationException($"Property '{propertyId}' was not found.");

        var item = new WishlistItem
        {
            WishlistId = wishlistId,
            PropertyId = propertyId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.WishlistItems.Add(item);

        return new WishlistItemData
        {
            Id = item.Id,
            WishlistId = wishlistId,
            PropertyId = propertyId,
            PropertyName = property.Name,
            MainImageUrl = GetMainImageUrl(property),
            City = property.City,
            Country = property.Country,
            CreatedAt = item.CreatedAt
        };
    }

    public async Task<bool> RemoveItemAsync(
        Guid wishlistId,
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.WishlistItems
            .FirstOrDefaultAsync(
                i => i.WishlistId == wishlistId && i.PropertyId == propertyId,
                cancellationToken);

        if (item is null)
        {
            return false;
        }

        _dbContext.WishlistItems.Remove(item);
        return true;
    }

    private WishlistData Map(Wishlist wishlist) => new()
    {
        Id = wishlist.Id,
        UserId = wishlist.UserId,
        Name = wishlist.Name,
        Items = wishlist.Items
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new WishlistItemData
            {
                Id = i.Id,
                WishlistId = i.WishlistId,
                PropertyId = i.PropertyId,
                PropertyName = i.Property?.Name ?? string.Empty,
                MainImageUrl = i.Property is null ? null : GetMainImageUrl(i.Property),
                City = i.Property?.City ?? string.Empty,
                Country = i.Property?.Country ?? string.Empty,
                CreatedAt = i.CreatedAt
            })
            .ToList()
    };

    private string? GetMainImageUrl(Property property) =>
        property.Images
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => _urls.ToPublicUrl(i.ImageUrl))
            .FirstOrDefault();
}
