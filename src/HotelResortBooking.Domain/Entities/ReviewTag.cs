namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class ReviewTag : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<ReviewTagAssignment> Assignments { get; set; } = new List<ReviewTagAssignment>();
}
