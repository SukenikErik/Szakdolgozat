namespace BudgetApp.Api.Entities;

public class Category
{
    public Guid CategoryId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#9E9E9E";

    public ApplicationUser User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<RecurringRule> RecurringRules { get; set; } = new List<RecurringRule>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}
