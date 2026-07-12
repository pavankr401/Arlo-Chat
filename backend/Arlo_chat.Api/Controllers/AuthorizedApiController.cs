using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;

namespace Arlo_chat.Api.Controllers;

public abstract class AuthorizedApiController : ControllerBase
{
    protected int CurrentUserId =>
        int.Parse(User.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
}
