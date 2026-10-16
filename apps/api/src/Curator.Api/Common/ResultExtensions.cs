using Curator.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Curator.Api.Common;

public static class ResultExtensions
{
    /// <summary>Returns <paramref name="onSuccess"/> for a value, or <c>ProblemDetails</c> for an error.</summary>
    public static IActionResult ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, IActionResult> onSuccess)
    {
        if (!result.IsFailure)
        {
            return onSuccess(result.Value);
        }

        var statusCode = result.Error.Kind switch
        {
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result.Error.Kind, "Unknown error kind."),
        };

        return controller.Problem(detail: result.Error.Message, statusCode: statusCode);
    }
}
