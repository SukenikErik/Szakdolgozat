using System.Net;
using System.Text;
using BudgetApp.Api.Entities;
using BudgetApp.Api.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace BudgetApp.Api.Services;

public enum SendConfirmationResult
{
    Sent,
    AlreadyConfirmed,
    TooSoon,
    Failed
}

public class EmailConfirmationService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IOptions<EmailOptions> options,
    IMemoryCache cache,
    ILogger<EmailConfirmationService> logger)
{
    private static readonly TimeSpan ResendDelay = TimeSpan.FromMinutes(1);

    /// <summary>Never throws on a delivery error, because the registration must succeed without the email too.</summary>
    public async Task<SendConfirmationResult> SendAsync(ApplicationUser user)
    {
        if (user.EmailConfirmed)
        {
            return SendConfirmationResult.AlreadyConfirmed;
        }

        // Limits how often an inbox can be filled with our emails.
        var cacheKey = $"confirmation-email:{user.Id}";
        if (cache.TryGetValue(cacheKey, out _))
        {
            return SendConfirmationResult.TooSoon;
        }

        cache.Set(cacheKey, true, ResendDelay);

        try
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await emailSender.SendAsync(CreateMessage(user, token));
            return SendConfirmationResult.Sent;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Sending the confirmation email to user {UserId} failed.", user.Id);
            cache.Remove(cacheKey);
            return SendConfirmationResult.Failed;
        }
    }

    public async Task<bool> ConfirmAsync(Guid userId, string encodedToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            return false;
        }

        return (await userManager.ConfirmEmailAsync(user, token)).Succeeded;
    }

    private EmailMessage CreateMessage(ApplicationUser user, string token)
    {
        // The token may contain characters that are not safe in a URL.
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = $"{options.Value.WebUrl.TrimEnd('/')}/confirm-email?userId={user.Id}&token={encodedToken}";
        var name = user.DisplayName ?? user.Email!;
        var deadline = (user.CreatedAt + UnconfirmedAccountPolicy.GracePeriod).ToString("yyyy-MM-dd");
        var notice = $"You can use your account right away, but if you do not confirm this address by {deadline} (UTC), "
            + "your account and all of its data will be permanently deleted.";
        const string ignore = "If you did not create this account, you can ignore this email.";

        var text = $"Hello {name},\n\nWelcome to Balance! Please confirm your email address:\n{link}\n\n{notice}\n\n{ignore}\n";

        // User input must be encoded, otherwise a name could inject HTML into the email.
        var html = $"""
            <div style="font-family: Arial, sans-serif; max-width: 520px; color: #0f172a; line-height: 1.6">
              <p>Hello {WebUtility.HtmlEncode(name)},</p>
              <p>Welcome to Balance! Please confirm your email address.</p>
              <p><a href="{WebUtility.HtmlEncode(link)}" style="display: inline-block; padding: 12px 24px; border-radius: 10px; background: #10b981; color: #0f172a; font-weight: bold; text-decoration: none">Confirm email</a></p>
              <p>{notice}</p>
              <p style="color: #526176">{ignore}</p>
            </div>
            """;

        return new EmailMessage(user.Email!, "Confirm your email address", text, html);
    }
}
