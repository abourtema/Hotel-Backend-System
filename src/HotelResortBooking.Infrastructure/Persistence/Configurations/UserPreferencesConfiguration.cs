using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Language).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.NotificationsEnabled).IsRequired();
        builder.Property(x => x.MarketingNotificationsEnabled).IsRequired();
        builder.Property(x => x.BiometricLoginEnabled).IsRequired();

        builder.ConfigureAuditableEntity();

        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
