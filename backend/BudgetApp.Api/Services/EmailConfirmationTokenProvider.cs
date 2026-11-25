using BudgetApp.Api.Entities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace BudgetApp.Api.Services;

/// <summary>
/// A separate token provider for the confirmation links: they are valid as long as the account may stay
/// unconfirmed, while the other Identity tokens (for example password reset) keep their own, shorter lifetime.
/// </summary>
public class EmailConfirmationTokenProvider(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<EmailConfirmationTokenProviderOptions> options,
    ILogger<EmailConfirmationTokenProvider> logger)
    : DataProtectorTokenProvider<ApplicationUser>(dataProtectionProvider, options, logger)
{
    public const string ProviderName = "EmailConfirmation";
}

public class EmailConfirmationTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public EmailConfirmationTokenProviderOptions()
    {
        Name = EmailConfirmationTokenProvider.ProviderName;
        TokenLifespan = UnconfirmedAccountPolicy.GracePeriod;
    }
}
