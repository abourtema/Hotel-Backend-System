using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class ReviewTagAssignmentConfiguration : IEntityTypeConfiguration<ReviewTagAssignment>
{
    public void Configure(EntityTypeBuilder<ReviewTagAssignment> builder)
    {
        builder.ToTable("ReviewTagAssignments");

        builder.HasKey(x => new { x.ReviewId, x.ReviewTagId });

        builder.HasOne(x => x.ReviewTag)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.ReviewTagId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ReviewTagId);
    }
}
