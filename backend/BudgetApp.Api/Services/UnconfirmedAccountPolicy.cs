using BudgetApp.Api.Entities;

namespace BudgetApp.Api.Services;

/// <summary>An account can be used without confirming the email, but only for 30 days (F-07).</summary>
public static class UnconfirmedAccountPolicy
{
    public const int GraceDays = 30;
    public static readonly TimeSpan GracePeriod = TimeSpan.FromDays(GraceDays);

    /// <summary>Null if the email is confirmed. A started day counts as a whole one.</summary>
    public static int? DaysLeft(ApplicationUser user, DateTime utcNow)
    {
        if (user.EmailConfirmed)
        {
            return null;
        }

        var left = user.CreatedAt + GracePeriod - utcNow;
        return Math.Max(0, (int)Math.Ceiling(left.TotalDays));
    }
}
