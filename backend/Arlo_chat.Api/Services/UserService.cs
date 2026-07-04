using Arlo_chat.Api.Data.Entities;
using Arlo_chat.Api.Models;
using Arlo_chat.Api.Data.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Arlo_chat.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IMapper _mapper;

    public UserService(IUserRepository users, IPasswordHasher<User> passwordHasher, IMapper mapper)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _mapper = mapper;
    }

    public async Task<RegisterOutcome> RegisterAsync(RegisterRequestModel request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Email))
            return new RegisterOutcome(false, false, "Username and email are required.");

        if (!IsValidEmail(request.Email))
            return new RegisterOutcome(false, false, "Email is not a valid email address.");

        var passwordError = PasswordValidator.Validate(request.Password);
        if (passwordError is not null)
            return new RegisterOutcome(false, false, passwordError);

        if (await _users.ExistsByUsernameOrEmailAsync(request.Username, request.Email))
            return new RegisterOutcome(false, true, "Username or email is already taken.");

        var user = _mapper.Map<User>(request);
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        try
        {
            await _users.AddAsync(user);
            await _users.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return new RegisterOutcome(false, true, "Username or email is already taken.");
        }

        return new RegisterOutcome(true, false, "Account created successfully.");
    }

    public async Task<User?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await _users.GetByUsernameAsync(username);
        if (user is null)
            return null;

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
            await _users.SaveChangesAsync();
        }

        return user;
    }

    public Task<User?> GetByIdAsync(int id) => _users.GetByIdAsync(id);

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new System.Net.Mail.MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
