namespace Arlo_chat.Api.Security;

public static class CookieNames
{
    public const string AccessToken = "access_token";
    public const string RefreshToken = "refresh_token";
    public const string Csrf = "XSRF-TOKEN";
    public const string CsrfHeaderName = "X-CSRF-Token";
    public const string RefreshPath = "/api/auth/refresh";
}
