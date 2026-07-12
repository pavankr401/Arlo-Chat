using System.IdentityModel.Tokens.Jwt;
using Arlo_chat.Api.Models;
using Arlo_chat.Api.Security;
using Arlo_chat.Api.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Arlo_chat.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;
    private readonly SameSiteMode _cookieSameSite;

    public AuthController(
        IUserService userService,
        ITokenService tokenService,
        IMapper mapper,
        IOptions<JwtOptions> jwtOptions,
        IOptions<SessionCookieOptions> cookieOptions)
    {
        _userService = userService;
        _tokenService = tokenService;
        _mapper = mapper;
        _jwtOptions = jwtOptions.Value;
        _cookieSameSite = Enum.TryParse<SameSiteMode>(cookieOptions.Value.SameSite, ignoreCase: true, out var mode)
            ? mode
            : SameSiteMode.Lax;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ResponseModel>> Register(RegisterRequestModel request)
    {
        var outcome = await _userService.RegisterAsync(request);
        if (!outcome.Success)
        {
            return outcome.Conflict
                ? Conflict(new ResponseModel(false, outcome.Message))
                : BadRequest(new ResponseModel(false, outcome.Message));
        }

        return Ok(new ResponseModel(true, outcome.Message));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestModel request)
    {
        var user = await _userService.ValidateCredentialsAsync(request.Username, request.Password);
        if (user is null)
            return Unauthorized(new ResponseModel(false, "Invalid username or password."));

        var familyId = Guid.NewGuid();
        var accessToken = _tokenService.GenerateAccessToken(user, familyId);
        var refreshToken = await _tokenService.IssueRefreshTokenAsync(user.Id, familyId);

        var csrfToken = IssueSessionCookies(accessToken, refreshToken.RawToken, refreshToken.ExpiresAt);

        return Ok(new AuthResponseDto(_mapper.Map<UserDto>(user), csrfToken));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthResponseDto>> Me()
    {
        var userId = GetUserIdFromClaims();
        var user = await _userService.GetByIdAsync(userId);
        if (user is null)
            return Unauthorized();

        // The frontend's JS can't read this cookie cross-origin (document.cookie is
        // scoped to the frontend's own origin), so the value it already holds server-side
        // is handed back in the body instead - re-hydrates the in-memory CSRF token on reload.
        var csrfToken = Request.Cookies[CookieNames.Csrf] ?? string.Empty;

        return Ok(new AuthResponseDto(_mapper.Map<UserDto>(user), csrfToken));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh()
    {
        var rawToken = Request.Cookies[CookieNames.RefreshToken];
        if (string.IsNullOrEmpty(rawToken))
            return Unauthorized(new ResponseModel(false, "Session expired. Please log in again."));

        var result = await _tokenService.ValidateAndRotateRefreshTokenAsync(rawToken);
        if (!result.Success || result.User is null)
        {
            ClearSessionCookies();
            return Unauthorized(new ResponseModel(false, "Session expired. Please log in again."));
        }

        var accessToken = _tokenService.GenerateAccessToken(result.User, result.FamilyId);
        var csrfToken = IssueSessionCookies(accessToken, result.NewRawToken!, result.NewExpiresAt);

        return Ok(new AuthResponseDto(_mapper.Map<UserDto>(result.User), csrfToken));
    }

    [Authorize]
    [ValidateCsrf]
    [HttpPost("logout")]
    public async Task<ActionResult<ResponseModel>> Logout()
    {
        var familyIdClaim = User.Claims.FirstOrDefault(c => c.Type == "fid")?.Value;
        if (Guid.TryParse(familyIdClaim, out var familyId))
            await _tokenService.RevokeFamilyAsync(familyId);

        ClearSessionCookies();
        return Ok(new ResponseModel(true, "Logged out."));
    }

    private int GetUserIdFromClaims()
    {
        var subClaim = User.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
        return int.Parse(subClaim);
    }

    private string IssueSessionCookies(string accessToken, string refreshToken, DateTime refreshExpiresAt)
    {
        var accessExpires = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var refreshExpires = new DateTimeOffset(DateTime.SpecifyKind(refreshExpiresAt, DateTimeKind.Utc));
        var csrfToken = _tokenService.GenerateCsrfToken();

        Response.Cookies.Append(CookieNames.AccessToken, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = _cookieSameSite,
            Path = "/",
            Expires = accessExpires
        });

        Response.Cookies.Append(CookieNames.Csrf, csrfToken, new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = _cookieSameSite,
            Path = "/",
            Expires = refreshExpires
        });

        Response.Cookies.Append(CookieNames.RefreshToken, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = _cookieSameSite,
            Path = CookieNames.RefreshPath,
            Expires = refreshExpires
        });

        return csrfToken;
    }

    private void ClearSessionCookies()
    {
        Response.Cookies.Delete(CookieNames.AccessToken, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(CookieNames.Csrf, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(CookieNames.RefreshToken, new CookieOptions { Path = CookieNames.RefreshPath });
    }
}
