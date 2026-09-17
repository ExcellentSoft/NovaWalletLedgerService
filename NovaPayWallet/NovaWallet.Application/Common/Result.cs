namespace NovaWallet.Application.Common;

/// <summary>
/// Category of failure returned by an application service. Controllers map this to an HTTP status code
/// (e.g. Validation -> 400, NotFound -> 404, Conflict -> 409, Unexpected -> 500).
/// </summary>
public enum ErrorType
{
    None,
    Validation,
    NotFound,
    Conflict,
    Unexpected
}

/// <summary>
/// Uniform, controller-friendly response envelope for application service calls. Services never throw
/// domain/infrastructure exceptions across the application boundary; they catch them and translate them
/// into a <see cref="ResponseResult{T}"/> so controllers only need to inspect <see cref="IsSuccess"/>/<see cref="ErrorType"/>.
/// </summary>
public class ResponseResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public ErrorType ErrorType { get; }
    public string? Error { get; }

    private ResponseResult(bool isSuccess, T? value, ErrorType errorType, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorType = errorType;
        Error = error;
    }

    public static ResponseResult<T> Success(T value) => new(true, value, ErrorType.None, null);

    public static ResponseResult<T> Failure(ErrorType errorType, string error) => new(false, default, errorType, error);
}
