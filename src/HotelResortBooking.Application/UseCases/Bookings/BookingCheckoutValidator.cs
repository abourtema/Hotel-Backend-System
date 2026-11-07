using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Bookings;

internal static class BookingCheckoutValidator
{
    public const int SpecialRequestsMaxLength = 500;

    public static Error? ValidateGuestDetails(
        BookingGuestInput guest,
        IReadOnlyList<BookingGuestInput> additionalGuests,
        int adults,
        int children,
        string? specialRequests)
    {
        if (specialRequests is { Length: > SpecialRequestsMaxLength })
        {
            return BusinessErrors.InvalidSpecialRequests();
        }

        if (!HasPrimaryGuest(guest))
        {
            return null;
        }

        var email = guest.Email?.Trim() ?? string.Empty;
        if (!email.Contains('@') || !email.Contains('.'))
        {
            return BusinessErrors.InvalidEmail();
        }

        var namedAdditional = additionalGuests
            .Where(g => !string.IsNullOrWhiteSpace(g.FirstName) || !string.IsNullOrWhiteSpace(g.LastName))
            .ToList();

        foreach (var extra in namedAdditional)
        {
            if (string.IsNullOrWhiteSpace(extra.FirstName) || string.IsNullOrWhiteSpace(extra.LastName))
            {
                return BusinessErrors.InvalidGuestDetails();
            }
        }

        var maxAdditional = Math.Max(0, adults + children - 1);
        if (namedAdditional.Count > maxAdditional)
        {
            return BusinessErrors.InvalidGuestDetails();
        }

        return null;
    }

    public static bool HasPrimaryGuest(BookingGuestInput guest)
        => !string.IsNullOrWhiteSpace(guest.FirstName)
           && !string.IsNullOrWhiteSpace(guest.LastName)
           && !string.IsNullOrWhiteSpace(guest.Email)
           && !string.IsNullOrWhiteSpace(guest.PhoneNumber);

    public static BookingGuestInput MergeWithProfile(BookingGuestInput guest, UserAccountData? user)
    {
        if (user is null)
        {
            return guest;
        }

        return new BookingGuestInput
        {
            FirstName = FirstNonEmpty(guest.FirstName, user.FirstName),
            LastName = FirstNonEmpty(guest.LastName, user.LastName),
            Email = FirstNonEmpty(guest.Email, user.Email),
            PhoneNumber = FirstNonEmpty(guest.PhoneNumber, user.PhoneNumber)
        };
    }

    public static Error? ValidateCheckoutPayment(BookingCheckoutPaymentInput payment)
    {
        if (payment.Method is null)
        {
            return null;
        }

        if (payment.Method is not (
            PaymentMethodType.CreditCard
            or PaymentMethodType.PayAtHotel
            or PaymentMethodType.MobileWallet))
        {
            return BusinessErrors.UnsupportedCheckoutPaymentMethod();
        }

        return null;
    }

    public static BookingStatus InitialBookingStatus(BookingCheckoutPaymentInput? payment)
        => payment?.Method is not null && payment.Method != PaymentMethodType.PayAtHotel
            ? BookingStatus.Completed
            : BookingStatus.Pending;

    public static bool ShouldRecordOnlinePayment(BookingCheckoutPaymentInput? payment)
        => payment?.Method is not null && payment.Method != PaymentMethodType.PayAtHotel;

    public static bool ShouldProcessPayment(BookingCheckoutPaymentInput? payment)
        => ShouldRecordOnlinePayment(payment);

    public static string? NormalizeSpecialRequests(string? specialRequests)
    {
        var trimmed = specialRequests?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    public static IReadOnlyList<CreateBookingGuestModel> BuildGuests(
        BookingGuestInput primary,
        IReadOnlyList<BookingGuestInput> additionalGuests)
    {
        if (!HasPrimaryGuest(primary))
        {
            return Array.Empty<CreateBookingGuestModel>();
        }

        var guests = new List<CreateBookingGuestModel>
        {
            new()
            {
                FirstName = primary.FirstName.Trim(),
                LastName = primary.LastName.Trim(),
                Email = primary.Email!.Trim(),
                PhoneNumber = primary.PhoneNumber!.Trim(),
                IsPrimary = true,
                SortOrder = 0
            }
        };

        var sort = 1;
        foreach (var extra in additionalGuests)
        {
            if (string.IsNullOrWhiteSpace(extra.FirstName) && string.IsNullOrWhiteSpace(extra.LastName))
            {
                continue;
            }

            guests.Add(new CreateBookingGuestModel
            {
                FirstName = extra.FirstName.Trim(),
                LastName = extra.LastName.Trim(),
                Email = string.IsNullOrWhiteSpace(extra.Email) ? null : extra.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(extra.PhoneNumber) ? null : extra.PhoneNumber.Trim(),
                IsPrimary = false,
                SortOrder = sort++
            });
        }

        return guests;
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;
}
