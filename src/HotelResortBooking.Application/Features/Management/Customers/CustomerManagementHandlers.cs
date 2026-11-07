using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.Features.Management.Customers;

public sealed class GetManagedCustomersQuery
{
    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public interface ICustomerManagementQueries
{
    Task<Result<PagedResult<ManagedCustomerListItem>>> ListAsync(
        GetManagedCustomersQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ManagedCustomerListItem>> GetAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class CustomerManagementHandlers : ICustomerManagementQueries
{
    private readonly IManagementQueryRepository _queries;

    public CustomerManagementHandlers(IManagementQueryRepository queries) => _queries = queries;

    public async Task<Result<PagedResult<ManagedCustomerListItem>>> ListAsync(
        GetManagedCustomersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.PageSize <= 0)
        {
            return Result.Failure<PagedResult<ManagedCustomerListItem>>(BusinessErrors.InvalidPagination());
        }

        var page = await _queries.SearchCustomersAsync(query.Search, query.Page, Math.Min(query.PageSize, 100), cancellationToken);
        return Result.Success(page);
    }

    public async Task<Result<ManagedCustomerListItem>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _queries.GetCustomerAsync(id, cancellationToken);
        return customer is null
            ? Result.Failure<ManagedCustomerListItem>(BusinessErrors.UserNotFound())
            : Result.Success(customer);
    }
}
