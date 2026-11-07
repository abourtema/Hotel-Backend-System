using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Services;
using HotelResortBooking.Application.UseCases.Auth;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Application.UseCases.Loyalty;
using HotelResortBooking.Application.UseCases.Map;
using HotelResortBooking.Application.UseCases.Payments;
using HotelResortBooking.Application.UseCases.Properties;
using HotelResortBooking.Application.UseCases.Reviews;
using HotelResortBooking.Application.UseCases.Rooms;
using HotelResortBooking.Application.UseCases.Search;
using HotelResortBooking.Application.UseCases.Upgrades;
using HotelResortBooking.Application.UseCases.Wishlist;
using HotelResortBooking.Application.Features.Management.Amenities;
using HotelResortBooking.Application.Features.Management.Bookings;
using HotelResortBooking.Application.Features.Management.Customers;
using HotelResortBooking.Application.Features.Management.Dashboard;
using HotelResortBooking.Application.Features.Management.Hotels;
using HotelResortBooking.Application.Features.Management.Loyalty;
using HotelResortBooking.Application.Features.Management.Payments;
using HotelResortBooking.Application.Features.Management.Reports;
using HotelResortBooking.Application.Features.Management.Reviews;
using HotelResortBooking.Application.Features.Management.Rooms;
using HotelResortBooking.Application.Features.Management.Upgrades;
using HotelResortBooking.Application.Features.Management.Users;
using Microsoft.Extensions.DependencyInjection;

namespace HotelResortBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IRoomAvailabilityService, RoomAvailabilityService>();

        // Search
        services.AddScoped<SearchPropertiesUseCase>();
        services.AddScoped<ISearchPropertiesUseCase>(sp => sp.GetRequiredService<SearchPropertiesUseCase>());
        services.AddScoped<IFilterPropertiesUseCase>(sp => sp.GetRequiredService<SearchPropertiesUseCase>());
        services.AddScoped<IGetPropertySearchResultsUseCase>(sp => sp.GetRequiredService<SearchPropertiesUseCase>());

        // Auth
        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<ILoginUserUseCase, LoginUserUseCase>();
        services.AddScoped<ISocialLoginUseCase, SocialLoginUseCase>();
        services.AddScoped<IContinueAsGuestUseCase, ContinueAsGuestUseCase>();
        services.AddScoped<IGetCurrentAccessStateUseCase, GetCurrentAccessStateUseCase>();
        services.AddScoped<ILogoutUserUseCase, LogoutUserUseCase>();

        // Properties / Rooms / Map
        services.AddScoped<IGetPropertyDetailsUseCase, GetPropertyDetailsUseCase>();
        services.AddScoped<IGetAvailableRoomsUseCase, GetAvailableRoomsUseCase>();
        services.AddScoped<ISearchPropertiesByLocationUseCase, SearchPropertiesByLocationUseCase>();

        // Bookings
        services.AddScoped<ICreateBookingDraftUseCase, CreateBookingDraftUseCase>();
        services.AddScoped<ICreateBookingUseCase, CreateBookingUseCase>();
        services.AddScoped<IGetBookingUseCase, GetBookingUseCase>();
        services.AddScoped<IGetUserBookingsUseCase, GetUserBookingsUseCase>();
        services.AddScoped<ICancelBookingUseCase, CancelBookingUseCase>();
        services.AddScoped<IConfirmBookingUseCase, ConfirmBookingUseCase>();
        services.AddScoped<ICompleteBookingUseCase, CompleteBookingUseCase>();
        services.AddScoped<IModifyBookingUseCase, ModifyBookingUseCase>();
        services.AddScoped<IGetBookingSummaryUseCase, GetBookingSummaryUseCase>();

        // Upgrades
        services.AddScoped<IGetAvailableUpgradesUseCase, GetAvailableUpgradesUseCase>();
        services.AddScoped<IAddBookingUpgradeUseCase, AddBookingUpgradeUseCase>();
        services.AddScoped<IRemoveBookingUpgradeUseCase, RemoveBookingUpgradeUseCase>();

        // Reviews / Wishlist
        services.AddScoped<IAddReviewUseCase, AddReviewUseCase>();
        services.AddScoped<IGetWishlistUseCase, GetWishlistUseCase>();
        services.AddScoped<IAddToWishlistUseCase, AddToWishlistUseCase>();
        services.AddScoped<IRemoveFromWishlistUseCase, RemoveFromWishlistUseCase>();

        // Loyalty
        services.AddScoped<IGetLoyaltyBalanceUseCase, GetLoyaltyBalanceUseCase>();
        services.AddScoped<ICalculatePointsRedemptionUseCase, CalculatePointsRedemptionUseCase>();
        services.AddScoped<IRedeemPointsUseCase, RedeemPointsUseCase>();
        services.AddScoped<IRefundPointsUseCase, RefundPointsUseCase>();
        services.AddScoped<IGetLoyaltyTransactionsUseCase, GetLoyaltyTransactionsUseCase>();
        services.AddScoped<IApplyLoyaltyPointsToBookingUseCase, ApplyLoyaltyPointsToBookingUseCase>();

        // Payments
        services.AddScoped<ICreatePaymentUseCase, CreatePaymentUseCase>();
        services.AddScoped<IConfirmPayAtHotelPaymentUseCase, ConfirmPayAtHotelPaymentUseCase>();
        services.AddScoped<IProcessPaymentUseCase, ProcessPaymentUseCase>();
        services.AddScoped<IGetPaymentUseCase, GetPaymentUseCase>();
        services.AddScoped<IHandlePaymentFailureUseCase, HandlePaymentFailureUseCase>();
        services.AddScoped<IPayBookingWithCardUseCase, PayBookingWithCardUseCase>();

        services.AddScoped<IAuthenticationService, AuthenticationService>();

        services.AddScoped<IGetManagementDashboardQuery, GetManagementDashboardHandler>();
        services.AddScoped<IGetHotelsForManagementQuery, GetHotelsForManagementHandler>();
        services.AddScoped<IGetHotelForManagementQuery, GetHotelForManagementHandler>();
        services.AddScoped<ICreateHotelCommand, CreateHotelHandler>();
        services.AddScoped<IUpdateHotelCommand, UpdateHotelHandler>();
        services.AddScoped<ISetHotelActiveCommand, SetHotelActiveHandler>();
        services.AddScoped<HotelImageHandlers>();
        services.AddScoped<IAddHotelImageCommand>(sp => sp.GetRequiredService<HotelImageHandlers>());
        services.AddScoped<IUpdateHotelImageCommand>(sp => sp.GetRequiredService<HotelImageHandlers>());
        services.AddScoped<IDeleteHotelImageCommand>(sp => sp.GetRequiredService<HotelImageHandlers>());
        services.AddScoped<ISetPrimaryHotelImageCommand>(sp => sp.GetRequiredService<HotelImageHandlers>());
        services.AddScoped<IReorderHotelImagesCommand>(sp => sp.GetRequiredService<HotelImageHandlers>());
        services.AddScoped<IAmenityManagementCommands, AmenityManagementHandlers>();
        services.AddScoped<IRoomManagementCommands, RoomManagementHandlers>();
        services.AddScoped<RoomImageHandlers>();
        services.AddScoped<IAddRoomImageCommand>(sp => sp.GetRequiredService<RoomImageHandlers>());
        services.AddScoped<IUpdateRoomImageCommand>(sp => sp.GetRequiredService<RoomImageHandlers>());
        services.AddScoped<IDeleteRoomImageCommand>(sp => sp.GetRequiredService<RoomImageHandlers>());
        services.AddScoped<ISetPrimaryRoomImageCommand>(sp => sp.GetRequiredService<RoomImageHandlers>());
        services.AddScoped<IReorderRoomImagesCommand>(sp => sp.GetRequiredService<RoomImageHandlers>());
        services.AddScoped<IUpgradeManagementCommands, UpgradeManagementHandlers>();
        services.AddScoped<IBookingManagementCommands, BookingManagementHandlers>();
        services.AddScoped<ICustomerManagementQueries, CustomerManagementHandlers>();
        services.AddScoped<IPaymentManagementCommands, PaymentManagementHandlers>();
        services.AddScoped<ILoyaltyManagementCommands, LoyaltyManagementHandlers>();
        services.AddScoped<IReviewManagementCommands, ReviewManagementHandlers>();
        services.AddScoped<IManagementUserCommands, ManagementUserHandlers>();
        services.AddScoped<IRoleManagementQueries, RoleManagementHandlers>();
        services.AddScoped<IManagementReportQueries, ManagementReportHandlers>();

        return services;
    }
}
