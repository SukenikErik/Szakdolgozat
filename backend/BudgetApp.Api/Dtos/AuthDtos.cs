using System.ComponentModel.DataAnnotations;

namespace BudgetApp.Api.Dtos;

public class RegisterRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    // The password strength rules are enforced by Identity (see Program.cs).
    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DisplayName { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; set; } = string.Empty;
}

public class ConfirmEmailRequest
{
    public Guid UserId { get; set; }

    [Required, StringLength(2000)]
    public string Token { get; set; } = string.Empty;
}

/// <param name="ConfirmationDaysLeft">Days until the unconfirmed account is deleted, null if it is confirmed.</param>
public record UserResponse(
    Guid UserId,
    string Email,
    string? DisplayName,
    DateTime CreatedAt,
    bool EmailConfirmed,
    int? ConfirmationDaysLeft);

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);
