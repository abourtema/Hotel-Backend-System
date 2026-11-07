using System.Security.Claims;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected bool CanManageGuestPayments()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        var permissions = User.FindAll(Permissions.ClaimType).Select(c => c.Value);
        return PermissionEvaluator.IsAllowed(roles, permissions, Permissions.BookingsUpdate)
            || PermissionEvaluator.IsAllowed(roles, permissions, Permissions.PaymentsRefund);
    }

    protected string? GetClientIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && !string.IsNullOrWhiteSpace(forwarded))
        {
            var firstIp = forwarded.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstIp))
            {
                return firstIp;
            }
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    protected IActionResult HandleResult(Result result, Func<IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is not null ? onSuccess() : Ok();
        }

        return MapError(result.Error!);
    }

    protected IActionResult HandleResult<T>(
        Result<T> result,
        Func<T, IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is not null ? onSuccess(result.Value) : Ok(result.Value);
        }

        return MapError(result.Error!);
    }

    private IActionResult MapError(Error error)
    {
        return error.Type switch
        {
            ErrorType.Validation => HandleValidationError(error),
            ErrorType.NotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: error.Message,
                type: "https://httpstatuses.com/404",
                extensions: new Dictionary<string, object?> { ["code"] = error.Code }),
            ErrorType.Authorization => HandleAuthorizationError(error),
            ErrorType.Conflict => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: error.Message,
                type: "https://httpstatuses.com/409",
                extensions: new Dictionary<string, object?> { ["code"] = error.Code }),
            ErrorType.Business => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Unprocessable Entity",
                detail: error.Message,
                type: "https://httpstatuses.com/422",
                extensions: new Dictionary<string, object?> { ["code"] = error.Code }),
            _ => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: error.Message,
                type: "https://httpstatuses.com/400",
                extensions: new Dictionary<string, object?> { ["code"] = error.Code })
        };
    }

    private IActionResult HandleValidationError(Error error)
    {
        if (error.ValidationErrors is not null && error.ValidationErrors.Count > 0)
        {
            var modelState = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            foreach (var kv in error.ValidationErrors)
            {
                foreach (var err in kv.Value)
                {
                    modelState.AddModelError(kv.Key, err);
                }
            }

            return ValidationProblem(
                modelStateDictionary: modelState,
                detail: error.Message,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error",
                type: "https://httpstatuses.com/400");
        }

        return Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Validation Error",
            detail: error.Message,
            type: "https://httpstatuses.com/400",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    private IActionResult HandleAuthorizationError(Error error)
    {
        var isForbidden = error.Code is "UnauthorizedBookingAccess" or "UnauthorizedPaymentAccess" or "Forbidden";
        var statusCode = isForbidden ? StatusCodes.Status403Forbidden : StatusCodes.Status401Unauthorized;
        var title = isForbidden ? "Forbidden" : "Unauthorized";

        return Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Message,
            type: $"https://httpstatuses.com/{statusCode}",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
