using System.Security.Cryptography;
using System.Text;
using Arlo_chat.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Arlo_chat.Api.Security;

/// <summary>
/// Double-submit CSRF check: the X-CSRF-Token header must match the non-HttpOnly CSRF cookie.
/// Only meaningful on state-changing endpoints reached via the auto-attaching auth cookies.
/// </summary>
public class ValidateCsrfAttribute : Attribute, IAsyncActionFilter
{
    public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var cookieValue = request.Cookies[CookieNames.Csrf];
        var headerValue = request.Headers[CookieNames.CsrfHeaderName].ToString();

        if (string.IsNullOrEmpty(cookieValue) || string.IsNullOrEmpty(headerValue) || !FixedTimeEquals(cookieValue, headerValue))
        {
            context.Result = new ObjectResult(new ResponseModel(false, "CSRF validation failed."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return Task.CompletedTask;
        }

        return next();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return bytesA.Length == bytesB.Length && CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
