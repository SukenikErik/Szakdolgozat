using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BudgetApp.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The id of the logged-in user, read from the token.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("The token has no 'sub' claim."));
}
