namespace BudgetApp.Api.Entities;

/// <summary>A tranzakció típusa (a témavezetői kiírás négy csoportja).</summary>
public enum TransactionType
{
    Income,
    FixedExpense,
    VariableExpense,
    Saving
}

/// <summary>Az ismétlődő szabály időegysége (5 naponta = IntervalCount 5 + Day).</summary>
public enum IntervalUnit
{
    Day,
    Week,
    Month
}
