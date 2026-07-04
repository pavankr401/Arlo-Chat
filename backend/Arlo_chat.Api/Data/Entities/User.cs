namespace Arlo_chat.Api.Data.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
