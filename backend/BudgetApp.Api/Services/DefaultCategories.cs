using BudgetApp.Api.Entities;

namespace BudgetApp.Api.Services;

/// <summary>The categories every new user starts with (F-04).</summary>
public static class DefaultCategories
{
    private static readonly (string Name, string Color)[] Items =
    [
        ("Groceries", "#4CAF50"),
        ("Housing", "#3F51B5"),
        ("Utilities", "#FF9800"),
        ("Transport", "#03A9F4"),
        ("Health", "#F44336"),
        ("Entertainment", "#9C27B0"),
        ("Dining out", "#FF5722"),
        ("Shopping", "#E91E63"),
        ("Salary", "#009688"),
        ("Other", "#9E9E9E")
    ];

    public static List<Category> Create() =>
        Items.Select(item => new Category { Name = item.Name, Color = item.Color }).ToList();
}
