using HotelResortBooking.Api.Contracts.Hotels;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Features.Management.Amenities;
using HotelResortBooking.Application.Features.Management.Hotels;
using HotelResortBooking.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/hotels")]
public sealed class ManagementHotelsController : ApiControllerBase
{
    private readonly IGetHotelsForManagementQuery _list;
    private readonly IGetHotelForManagementQuery _get;
    private readonly ICreateHotelCommand _create;
    private readonly IUpdateHotelCommand _update;
    private readonly ISetHotelActiveCommand _setActive;
    private readonly IAddHotelImageCommand _addImage;
    private readonly IUpdateHotelImageCommand _updateImage;
    private readonly IDeleteHotelImageCommand _deleteImage;
    private readonly ISetPrimaryHotelImageCommand _setPrimary;
    private readonly IReorderHotelImagesCommand _reorder;
    private readonly IAmenityManagementCommands _amenities;

    public ManagementHotelsController(
        IGetHotelsForManagementQuery list,
        IGetHotelForManagementQuery get,
        ICreateHotelCommand create,
        IUpdateHotelCommand update,
        ISetHotelActiveCommand setActive,
        IAddHotelImageCommand addImage,
        IUpdateHotelImageCommand updateImage,
        IDeleteHotelImageCommand deleteImage,
        ISetPrimaryHotelImageCommand setPrimary,
        IReorderHotelImagesCommand reorder,
        IAmenityManagementCommands amenities)
    {
        _list = list;
        _get = get;
        _create = create;
        _update = update;
        _setActive = setActive;
        _addImage = addImage;
        _updateImage = updateImage;
        _deleteImage = deleteImage;
        _setPrimary = setPrimary;
        _reorder = reorder;
        _amenities = amenities;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.HotelsView)]
    [ProducesResponseType(typeof(PagedResult<ManagedHotelListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => HandleResult(await _list.ExecuteAsync(new GetHotelsForManagementQuery
        {
            Search = search,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.HotelsView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _get.ExecuteAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.HotelsCreate)]
    public async Task<IActionResult> Create([FromBody] SaveHotelRequest request, CancellationToken cancellationToken)
        => HandleResult(await _create.ExecuteAsync(request, cancellationToken), created => CreatedAtAction(nameof(Get), new { id = created.Id }, created));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.HotelsUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveHotelRequest request, CancellationToken cancellationToken)
        => HandleResult(await _update.ExecuteAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Permissions.HotelsUpdate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _setActive.ExecuteAsync(id, true, cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Permissions.HotelsDelete)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _setActive.ExecuteAsync(id, false, cancellationToken));

    [HttpPost("{hotelId:guid}/images")]
    [Authorize(Policy = Permissions.HotelsUpdate)]
    [RequestSizeLimit(6_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
    public async Task<IActionResult> AddImage(Guid hotelId, [FromForm] HotelImageForm form, CancellationToken cancellationToken)
        => HandleResult(await _addImage.ExecuteAsync(hotelId, form.ToUseCaseRequest(), cancellationToken));

    [HttpPut("{hotelId:guid}/images/{imageId:guid}")]
    [Authorize(Policy = Permissions.HotelsUpdate)]
    [RequestSizeLimit(6_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
    public async Task<IActionResult> UpdateImage(Guid hotelId, Guid imageId, [FromForm] HotelImageForm form, CancellationToken cancellationToken)
        => HandleResult(await _updateImage.ExecuteAsync(hotelId, imageId, form.ToUseCaseRequest(), cancellationToken));

    [HttpDelete("{hotelId:guid}/images/{imageId:guid}")]
    [Authorize(Policy = Permissions.HotelsUpdate)]
    public async Task<IActionResult> DeleteImage(Guid hotelId, Guid imageId, CancellationToken cancellationToken)
        => HandleResult(await _deleteImage.ExecuteAsync(hotelId, imageId, cancellationToken));

    [HttpPost("{hotelId:guid}/images/{imageId:guid}/primary")]
    [Authorize(Policy = Permissions.HotelsUpdate)]
    public async Task<IActionResult> SetPrimary(Guid hotelId, Guid imageId, CancellationToken cancellationToken)
        => HandleResult(await _setPrimary.ExecuteAsync(hotelId, imageId, cancellationToken));

    [HttpPut("{hotelId:guid}/images/reorder")]
    [Authorize(Policy = Permissions.HotelsUpdate)]
    public async Task<IActionResult> Reorder(Guid hotelId, [FromBody] Guid[] imageIds, CancellationToken cancellationToken)
        => HandleResult(await _reorder.ExecuteAsync(hotelId, imageIds, cancellationToken));

    [HttpPost("{hotelId:guid}/amenities/{amenityId:guid}")]
    [Authorize(Policy = Permissions.AmenitiesUpdate)]
    public async Task<IActionResult> AssignAmenity(Guid hotelId, Guid amenityId, CancellationToken cancellationToken)
        => HandleResult(await _amenities.AssignToPropertyAsync(hotelId, amenityId, cancellationToken));

    [HttpDelete("{hotelId:guid}/amenities/{amenityId:guid}")]
    [Authorize(Policy = Permissions.AmenitiesUpdate)]
    public async Task<IActionResult> RemoveAmenity(Guid hotelId, Guid amenityId, CancellationToken cancellationToken)
        => HandleResult(await _amenities.RemoveFromPropertyAsync(hotelId, amenityId, cancellationToken));
}
