namespace BudgetApp.Api.Entities;

public class Transaction
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string? Note { get; set; }

    public Guid? CategoryId { get; set; }
    public Guid? GoalId { get; set; }   // csak Saving típusnál
    public Guid? RuleId { get; set; }   // csak FixedExpense típusnál

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public Category? Category { get; set; }
    public Goal? Goal { get; set; }
    public RecurringRule? Rule { get; set; }
}
