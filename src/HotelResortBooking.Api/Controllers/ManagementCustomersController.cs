using HotelResortBooking.Application.Features.Management.Customers;
using HotelResortBooking.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/customers")]
public sealed class ManagementCustomersController : ApiControllerBase
{
    private readonly ICustomerManagementQueries _customers;

    public ManagementCustomersController(ICustomerManagementQueries customers) => _customers = customers;

    [HttpGet]
    [Authorize(Policy = Permissions.CustomersView)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? name,
        [FromQuery] string? email,
        [FromQuery] string? phone,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => HandleResult(await _customers.ListAsync(new GetManagedCustomersQuery
        {
            Search = search ?? name ?? email ?? phone,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.CustomersView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _customers.GetAsync(id, cancellationToken));
}
