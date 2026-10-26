using TaskBoard.Domain.Common;

namespace TaskBoard.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : result.Error.ToProblem();

    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null) =>
        result.IsSuccess
            ? onSuccess?.Invoke(result.Value) ?? Results.Ok(result.Value)
            : result.Error.ToProblem();

    public static IResult ToProblem(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Failure => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        var extensions = error is ValidationError validationError
            ? new Dictionary<string, object?> { ["errors"] = validationError.Errors }
            : null;

        return Results.Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Description,
            extensions: extensions);
    }
}
