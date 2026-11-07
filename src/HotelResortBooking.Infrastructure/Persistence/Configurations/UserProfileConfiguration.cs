using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProfilePhotoUrl).HasMaxLength(2048);
        builder.Property(x => x.DateOfBirth).HasColumnType("date");
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.PreferredLanguage).HasMaxLength(16).IsRequired();
        builder.Property(x => x.PreferredCurrency).HasMaxLength(3).IsRequired();

        builder.ConfigureAuditableEntity();

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
