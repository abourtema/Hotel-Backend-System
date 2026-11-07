namespace HotelResortBooking.Application.Common.Results;

public sealed class Error
{
    public Error(
        string code,
        string message,
        ErrorType type,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        Code = code;
        Message = message;
        Type = type;
        ValidationErrors = validationErrors;
    }

    public string Code { get; }

    public string Message { get; }

    public ErrorType Type { get; }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string[]>? errors = null)
        => new(code, message, ErrorType.Validation, errors);

    public static Error NotFound(string code, string message)
        => new(code, message, ErrorType.NotFound);

    public static Error Authorization(string code, string message)
        => new(code, message, ErrorType.Authorization);

    public static Error Conflict(string code, string message)
        => new(code, message, ErrorType.Conflict);

    public static Error Business(string code, string message)
        => new(code, message, ErrorType.Business);
}
