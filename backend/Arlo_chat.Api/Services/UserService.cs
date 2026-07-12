using Arlo_chat.Api.Data;
using Arlo_chat.Api.Data.Entities;
using Arlo_chat.Api.Models;
using Arlo_chat.Api.Data.Repositories;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Arlo_chat.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IFriendshipRepository _friendships;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IMapper _mapper;

    public UserService(IUserRepository users, IFriendshipRepository friendships, IPasswordHasher<User> passwordHasher, IMapper mapper)
    {
        _users = users;
        _friendships = friendships;
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
        user.LastActiveAt = DateTime.UtcNow;

        try
        {
            await _users.AddAsync(user);
            await _users.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (PostgresErrorHelper.IsUniqueViolation(ex))
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

    public async Task TouchLastActiveAsync(int userId)
    {
        var user = await _users.GetByIdAsync(userId);
        if (user is null || DateTime.UtcNow - user.LastActiveAt <= TimeSpan.FromHours(1))
            return;

        user.LastActiveAt = DateTime.UtcNow;
        await _users.SaveChangesAsync();
    }

    public async Task<List<FriendUserDto>> SearchUsersAsync(string searchQuery, int currentUserId, int lastRecentUserId, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return new List<FriendUserDto>();

        var candidates = await _users.SearchByPrefixAsync(searchQuery, currentUserId, lastRecentUserId, pageSize);
        var results = new List<FriendUserDto>(candidates.Count);

        foreach (var candidate in candidates)
        {
            var low = Math.Min(currentUserId, candidate.Id);
            var high = Math.Max(currentUserId, candidate.Id);
            var friendship = await _friendships.GetByUserPairAsync(low, high);
            var uiStatus = ToUiStatus(friendship, currentUserId);
            results.Add(new FriendUserDto(candidate.Id, candidate.Username, candidate.Email, uiStatus, candidate.LastActiveAt));
        }

        return results;
    }

    public async Task<ResponseModel> AddFriendAsync(int requesterId, int requesteeId)
    {
        if (requesterId == requesteeId)
            return new ResponseModel(false, "You can't add yourself as a friend.");

        var low = Math.Min(requesterId, requesteeId);
        var high = Math.Max(requesterId, requesteeId);
        var friendship = await _friendships.GetByUserPairAsync(low, high);

        if (friendship is not null && friendship.Status == FriendRequestStatus.Accepted)
            return new ResponseModel(false, "You're already connected.");

        if (friendship is not null && friendship.Status == FriendRequestStatus.Pending)
        {
            return friendship.RequesterId == requesterId
                ? new ResponseModel(false, "Friend request already sent.")
                : new ResponseModel(false, "This user already sent you a friend request.");
        }

        try
        {
            if (friendship is not null)
            {
                friendship.RequesterId = requesterId;
                friendship.RequesteeId = requesteeId;
                friendship.Status = FriendRequestStatus.Pending;
                friendship.CreatedDate = DateTime.UtcNow;
            }
            else
            {
                friendship = new Friendship
                {
                    RequesterId = requesterId,
                    RequesteeId = requesteeId,
                    UserIdLow = low,
                    UserIdHigh = high,
                    Status = FriendRequestStatus.Pending,
                    CreatedDate = DateTime.UtcNow
                };
                await _friendships.AddAsync(friendship);
            }

            await _friendships.SaveChangesAsync();
            return new ResponseModel(true, "Friend request sent.");
        }
        catch (DbUpdateException ex) when (PostgresErrorHelper.IsUniqueViolation(ex))
        {
            return new ResponseModel(false, "A friend request already exists between you two.");
        }
    }

    public async Task<ResponseModel> ManageFriendAsync(int performerUserId, int targetUserId, FriendRequestStatus status)
    {
        if (performerUserId == targetUserId)
            return new ResponseModel(false, "Invalid request.");

        var low = Math.Min(performerUserId, targetUserId);
        var high = Math.Max(performerUserId, targetUserId);
        var friendship = await _friendships.GetByUserPairAsync(low, high);

        if (friendship is null)
            return new ResponseModel(false, "No connection exists to perform this action.");

        if (friendship.Status == FriendRequestStatus.Pending && status is FriendRequestStatus.Accepted or FriendRequestStatus.Rejected)
        {
            if (friendship.RequesteeId != performerUserId)
                return new ResponseModel(false, "Unauthorized action.");

            friendship.Status = status;
            await _friendships.SaveChangesAsync();
            return new ResponseModel(true, status == FriendRequestStatus.Accepted ? "Request accepted." : "Request rejected.");
        }

        if (friendship.Status == FriendRequestStatus.Pending && status == FriendRequestStatus.Cancelled)
        {
            if (friendship.RequesterId != performerUserId)
                return new ResponseModel(false, "Unauthorized action.");

            friendship.Status = FriendRequestStatus.Cancelled;
            await _friendships.SaveChangesAsync();
            return new ResponseModel(true, "Request cancelled.");
        }

        if (friendship.Status == FriendRequestStatus.Accepted && status == FriendRequestStatus.Removed)
        {
            friendship.Status = FriendRequestStatus.Removed;
            await _friendships.SaveChangesAsync();
            return new ResponseModel(true, "Friend removed.");
        }

        return new ResponseModel(false, "Unauthorized action.");
    }

    public async Task<List<FriendUserDto>> FetchFriendsAsync(int userId, int lastRecentUserId, int pageSize)
    {
        var rows = await _friendships.GetAcceptedFriendsPageAsync(userId, lastRecentUserId, pageSize);
        return rows.Select(r => new FriendUserDto(r.UserId, r.Username, r.Email, UiFriendRequestStatus.Accepted, r.LastActiveAt)).ToList();
    }

    public async Task<List<FriendRequestDto>> FetchFriendRequestsAsync(int userId, int lastRecentFriendshipId, int pageSize)
    {
        var rows = await _friendships.GetPendingRequestsPageAsync(userId, lastRecentFriendshipId, pageSize);
        return rows.Select(r => new FriendRequestDto(r.FriendshipId, r.RequesterId, r.RequesteeId, r.Username, r.Email, r.CreatedDate)).ToList();
    }

    private static UiFriendRequestStatus ToUiStatus(Friendship? friendship, int currentUserId) => friendship switch
    {
        null => UiFriendRequestStatus.None,
        { Status: FriendRequestStatus.Pending } f when f.RequesterId == currentUserId => UiFriendRequestStatus.RequestSent,
        { Status: FriendRequestStatus.Pending } => UiFriendRequestStatus.RequestReceived,
        { Status: FriendRequestStatus.Accepted } => UiFriendRequestStatus.Accepted,
        _ => UiFriendRequestStatus.None
    };

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
}
