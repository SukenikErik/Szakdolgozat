using BudgetApp.Api.Dtos;
using BudgetApp.Api.Entities;
using Microsoft.AspNetCore.Identity;

namespace BudgetApp.Api.Services;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    TokenService tokenService,
    EmailConfirmationService emailConfirmation,
    TimeProvider timeProvider)
{
    /// <summary>On failure the response is null and the result holds the Identity errors.</summary>
    public async Task<(IdentityResult Result, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim();
        var user = new ApplicationUser
        {
            // The email is the login name, so the unique index on the user name
            // also guarantees unique emails in the database.
            UserName = email,
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim(),
            // EF Core saves the categories together with the user, in one database transaction.
            Categories = DefaultCategories.Create()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return (result, null);
        }

        // A failed delivery is only logged, the user can request the email again.
        await emailConfirmation.SendAsync(user);
        return (result, ToAuthResponse(user));
    }

    /// <summary>Returns null if the credentials are wrong or the account is locked out.</summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return null;
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        return result.Succeeded ? ToAuthResponse(user) : null;
    }

    public async Task<UserResponse?> GetUserAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToUserResponse(user);
    }

    /// <summary>Returns null if the user does not exist any more.</summary>
    public async Task<SendConfirmationResult?> ResendConfirmationAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await emailConfirmation.SendAsync(user);
    }

    private AuthResponse ToAuthResponse(ApplicationUser user)
    {
        var (token, expiresAt) = tokenService.CreateToken(user);
        return new AuthResponse(token, expiresAt, ToUserResponse(user));
    }

    private UserResponse ToUserResponse(ApplicationUser user) => new(
        user.Id,
        user.Email!,
        user.DisplayName,
        user.CreatedAt,
        user.EmailConfirmed,
        UnconfirmedAccountPolicy.DaysLeft(user, timeProvider.GetUtcNow().UtcDateTime));
}
