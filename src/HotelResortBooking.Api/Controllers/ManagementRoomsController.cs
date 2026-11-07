using HotelResortBooking.Api.Contracts.Rooms;
using HotelResortBooking.Application.Features.Management.Rooms;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/rooms")]
public sealed class ManagementRoomsController : ApiControllerBase
{
    private readonly IRoomManagementCommands _rooms;
    private readonly IAddRoomImageCommand _addImage;
    private readonly IUpdateRoomImageCommand _updateImage;
    private readonly IDeleteRoomImageCommand _deleteImage;
    private readonly ISetPrimaryRoomImageCommand _setPrimary;
    private readonly IReorderRoomImagesCommand _reorder;

    public ManagementRoomsController(
        IRoomManagementCommands rooms,
        IAddRoomImageCommand addImage,
        IUpdateRoomImageCommand updateImage,
        IDeleteRoomImageCommand deleteImage,
        ISetPrimaryRoomImageCommand setPrimary,
        IReorderRoomImagesCommand reorder)
    {
        _rooms = rooms;
        _addImage = addImage;
        _updateImage = updateImage;
        _deleteImage = deleteImage;
        _setPrimary = setPrimary;
        _reorder = reorder;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.RoomsView)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? propertyId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => HandleResult(await _rooms.ListAsync(new GetManagedRoomsQuery
        {
            PropertyId = propertyId,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.RoomsView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _rooms.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.RoomsCreate)]
    public async Task<IActionResult> Create([FromBody] SaveRoomRequest request, CancellationToken cancellationToken)
        => HandleResult(await _rooms.CreateAsync(request, cancellationToken), created => CreatedAtAction(nameof(Get), new { id = created.Id }, created));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.RoomsUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveRoomRequest request, CancellationToken cancellationToken)
        => HandleResult(await _rooms.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Permissions.RoomsUpdate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _rooms.SetActiveAsync(id, true, cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Permissions.RoomsDelete)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _rooms.SetActiveAsync(id, false, cancellationToken));

    [HttpGet("{roomTypeId:guid}/availability")]
    [Authorize(Policy = Permissions.RoomsView)]
    public async Task<IActionResult> GetAvailability(
        Guid roomTypeId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
        => HandleResult(await _rooms.GetAvailabilityAsync(roomTypeId, from, to, cancellationToken));

    [HttpPut("{roomTypeId:guid}/availability")]
    [Authorize(Policy = Permissions.RoomsUpdate)]
    public async Task<IActionResult> UpdateAvailability(
        Guid roomTypeId,
        [FromBody] List<RoomInventoryUpdateItem> items,
        CancellationToken cancellationToken)
        => HandleResult(await _rooms.UpdateAvailabilityAsync(roomTypeId, items, cancellationToken));

    [HttpPost("{roomTypeId:guid}/images")]
    [Authorize(Policy = Permissions.RoomsUpdate)]
    [RequestSizeLimit(6_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
    public async Task<IActionResult> AddImage(Guid roomTypeId, [FromForm] RoomImageForm form, CancellationToken cancellationToken)
        => HandleResult(await _addImage.ExecuteAsync(roomTypeId, form.ToUseCaseRequest(), cancellationToken));

    [HttpPut("{roomTypeId:guid}/images/{imageId:guid}")]
    [Authorize(Policy = Permissions.RoomsUpdate)]
    [RequestSizeLimit(6_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
    public async Task<IActionResult> UpdateImage(Guid roomTypeId, Guid imageId, [FromForm] RoomImageForm form, CancellationToken cancellationToken)
        => HandleResult(await _updateImage.ExecuteAsync(roomTypeId, imageId, form.ToUseCaseRequest(), cancellationToken));

    [HttpDelete("{roomTypeId:guid}/images/{imageId:guid}")]
    [Authorize(Policy = Permissions.RoomsUpdate)]
    public async Task<IActionResult> DeleteImage(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken)
        => HandleResult(await _deleteImage.ExecuteAsync(roomTypeId, imageId, cancellationToken));

    [HttpPost("{roomTypeId:guid}/images/{imageId:guid}/primary")]
    [Authorize(Policy = Permissions.RoomsUpdate)]
    public async Task<IActionResult> SetPrimary(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken)
        => HandleResult(await _setPrimary.ExecuteAsync(roomTypeId, imageId, cancellationToken));

    [HttpPut("{roomTypeId:guid}/images/reorder")]
    [Authorize(Policy = Permissions.RoomsUpdate)]
    public async Task<IActionResult> Reorder(Guid roomTypeId, [FromBody] Guid[] imageIds, CancellationToken cancellationToken)
        => HandleResult(await _reorder.ExecuteAsync(roomTypeId, imageIds, cancellationToken));
}
