namespace BudgetApp.Api.Entities;

/// <summary>
/// Havi keret egy kategóriára. Szándékosan nincs UserId: a tulajdonos a kategóriából egyértelmű.
/// </summary>
public class Budget
{
    public Guid BudgetId { get; set; }
    public Guid CategoryId { get; set; }
    public short BudgetYear { get; set; }
    public short BudgetMonth { get; set; }
    public decimal LimitAmount { get; set; }

    public Category Category { get; set; } = null!;
}
