namespace Arlo_chat.Api.Security;

public class SessionCookieOptions
{
    public string SameSite { get; set; } = "Lax";
}
