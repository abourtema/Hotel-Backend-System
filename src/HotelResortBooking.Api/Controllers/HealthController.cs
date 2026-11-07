using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

/// <summary>
/// Health check endpoint used to verify the API host starts successfully.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "HotelResortBooking.Api",
            timestampUtc = DateTimeOffset.UtcNow
        });
    }
}
