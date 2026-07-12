namespace Arlo_chat.Api.Models;

public record AuthResponseDto(UserDto User, string CsrfToken);
