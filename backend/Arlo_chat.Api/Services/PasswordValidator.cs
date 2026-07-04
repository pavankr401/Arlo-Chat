namespace Arlo_chat.Api.Services;

public static class PasswordValidator
{
    public const int MinLength = 8;
    public const int MaxLength = 10;

    public static string? Validate(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength || password.Length > MaxLength)
            return $"Password must be between {MinLength} and {MaxLength} characters long.";

        if (!password.Any(char.IsLetter))
            return "Password must contain at least one letter.";

        if (!password.Any(char.IsDigit))
            return "Password must contain at least one number.";

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            return "Password must contain at least one symbol.";

        return null;
    }
}
