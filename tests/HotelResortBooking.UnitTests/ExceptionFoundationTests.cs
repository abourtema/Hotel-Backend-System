using HotelResortBooking.Application.Exceptions;
using Xunit;

namespace HotelResortBooking.UnitTests;

public sealed class ExceptionFoundationTests
{
    [Fact]
    public void NotFoundException_ContainsEntityNameAndKey()
    {
        var exception = new NotFoundException("Resource", 42);

        Assert.Equal("Resource", exception.Name);
        Assert.Equal(42, exception.Key);
        Assert.Contains("Resource", exception.Message);
    }

    [Fact]
    public void ValidationException_StoresErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Name"] = ["Name is required."]
        };

        var exception = new ValidationException(errors);

        Assert.Same(errors, exception.Errors);
    }
}
