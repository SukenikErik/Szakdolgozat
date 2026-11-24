using Microsoft.AspNetCore.Identity;

namespace BudgetApp.Api.Entities;

/// <summary>
/// A felhasználó. Az Identity kezeli az e-mailt és a jelszó hash-t,
/// ide csak a saját mezőink kerülnek.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<RecurringRule> RecurringRules { get; set; } = new List<RecurringRule>();
    public ICollection<Goal> Goals { get; set; } = new List<Goal>();
}
