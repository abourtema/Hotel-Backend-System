using HotelResortBooking.Application;
using HotelResortBooking.Domain.Common;
using NetArchTest.Rules;
using Xunit;

namespace HotelResortBooking.ArchitectureTests;

public sealed class DependencyRuleTests
{
    private const string DomainNamespace = "HotelResortBooking.Domain";
    private const string ApplicationNamespace = "HotelResortBooking.Application";
    private const string InfrastructureNamespace = "HotelResortBooking.Infrastructure";
    private const string ApiNamespace = "HotelResortBooking.Api";

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(Entity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(typeof(DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(typeof(HotelResortBooking.Infrastructure.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_EfCore_Or_AspNetCore()
    {
        var result = Types.InAssembly(typeof(Entity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatFailures(result));
    }

    private static string FormatFailures(TestResult result)
    {
        if (result.FailingTypes is null || !result.FailingTypes.Any())
        {
            return "Architecture rule failed.";
        }

        return "Architecture rule failed for: " +
               string.Join(", ", result.FailingTypes.Select(t => t.FullName));
    }
}
