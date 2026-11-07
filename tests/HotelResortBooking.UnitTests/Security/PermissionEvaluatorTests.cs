using HotelResortBooking.Application.Security;
using Xunit;

namespace HotelResortBooking.UnitTests.Security;

public sealed class PermissionEvaluatorTests
{
    [Fact]
    public void CustomerRole_IsDeniedManagementPermission()
    {
        var allowed = PermissionEvaluator.IsAllowed([Roles.User], [], Permissions.HotelsView);
        Assert.False(allowed);
    }

    [Fact]
    public void MissingPermission_IsDenied()
    {
        var allowed = PermissionEvaluator.IsAllowed(
            [Roles.HotelManager],
            [Permissions.HotelsView],
            Permissions.HotelsDelete);
        Assert.False(allowed);
    }

    [Fact]
    public void MatchingPermission_IsAllowed()
    {
        var allowed = PermissionEvaluator.IsAllowed(
            [Roles.HotelManager],
            [Permissions.HotelsView],
            Permissions.HotelsView);
        Assert.True(allowed);
    }

    [Fact]
    public void SuperAdmin_IsAllowedAllPermissions()
    {
        Assert.True(PermissionEvaluator.IsAllowed([Roles.SuperAdmin], [], Permissions.PaymentsRefund));
        Assert.True(PermissionEvaluator.IsAllowed([Roles.Admin], [], Permissions.RolesManage));
    }
}
