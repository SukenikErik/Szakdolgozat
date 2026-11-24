using BudgetApp.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BudgetApp.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<RecurringRule> RecurringRules => Set<RecurringRule>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Budget> Budgets => Set<Budget>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ---------- Users (Identity felhasználó) ----------
        builder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("Users");
            e.Property(u => u.Id).HasColumnName("UserId");
            e.Property(u => u.DisplayName).HasMaxLength(100);
            e.Property(u => u.CreatedAt).HasDefaultValueSql("now()");
        });

        // ---------- Categories ----------
        builder.Entity<Category>(e =>
        {
            e.ToTable("Categories", tb =>
                tb.HasCheckConstraint("ck_categories_color", "\"Color\" ~ '^#[0-9A-Fa-f]{6}$'"));
            e.HasKey(c => c.CategoryId);
            e.Property(c => c.Name).HasMaxLength(50).IsRequired();
            e.Property(c => c.Color).HasMaxLength(7).IsFixedLength().IsRequired().HasDefaultValue("#9E9E9E");
            e.HasIndex(c => new { c.UserId, c.Name }).IsUnique();

            e.HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- Goals ----------
        builder.Entity<Goal>(e =>
        {
            e.ToTable("Goals", tb =>
                tb.HasCheckConstraint("ck_goals_target_amount", "\"TargetAmount\" > 0"));
            e.HasKey(g => g.GoalId);
            e.Property(g => g.Name).HasMaxLength(100).IsRequired();
            e.Property(g => g.TargetAmount).HasPrecision(18, 2);
            e.Property(g => g.CreatedAt).HasDefaultValueSql("now()");

            e.HasOne(g => g.User)
                .WithMany(u => u.Goals)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- RecurringRules ----------
        builder.Entity<RecurringRule>(e =>
        {
            e.ToTable("RecurringRules", tb =>
            {
                tb.HasCheckConstraint("ck_rules_amount", "\"Amount\" > 0");
                tb.HasCheckConstraint("ck_rules_interval_unit", "\"IntervalUnit\" IN ('Day', 'Week', 'Month')");
                tb.HasCheckConstraint("ck_rules_interval_count", "\"IntervalCount\" >= 1");
                tb.HasCheckConstraint("ck_rules_dates", "\"EndDate\" IS NULL OR \"EndDate\" >= \"StartDate\"");
            });
            e.HasKey(r => r.RuleId);
            e.Property(r => r.Name).HasMaxLength(100).IsRequired();
            e.Property(r => r.Amount).HasPrecision(18, 2);
            e.Property(r => r.IntervalUnit).HasConversion<string>().HasMaxLength(5);

            e.HasOne(r => r.User)
                .WithMany(u => u.RecurringRules)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Kategória csak akkor törölhető, ha nincs hozzá szabály (NO ACTION).
            e.HasOne(r => r.Category)
                .WithMany(c => c.RecurringRules)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ---------- Transactions ----------
        builder.Entity<Transaction>(e =>
        {
            e.ToTable("Transactions", tb =>
            {
                tb.HasCheckConstraint("ck_transactions_amount", "\"Amount\" > 0");
                tb.HasCheckConstraint("ck_transactions_type",
                    "\"TransactionType\" IN ('Income', 'FixedExpense', 'VariableExpense', 'Saving')");
                tb.HasCheckConstraint("ck_tr_goal_only_saving",
                    "\"GoalId\" IS NULL OR \"TransactionType\" = 'Saving'");
                tb.HasCheckConstraint("ck_tr_rule_only_fixed",
                    "\"RuleId\" IS NULL OR \"TransactionType\" = 'FixedExpense'");
            });
            e.HasKey(t => t.TransactionId);
            e.Property(t => t.TransactionType).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.Note).HasMaxLength(255);
            e.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
            e.Property(t => t.UpdatedAt).HasDefaultValueSql("now()");

            e.HasIndex(t => new { t.UserId, t.TransactionDate });
            e.HasIndex(t => t.CategoryId);
            e.HasIndex(t => t.GoalId);
            // Ugyanarra a napra ugyanabból a szabályból nem jöhet létre kétszer tranzakció.
            e.HasIndex(t => new { t.RuleId, t.TransactionDate }).IsUnique();

            e.HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(t => t.Goal)
                .WithMany(g => g.Transactions)
                .HasForeignKey(t => t.GoalId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(t => t.Rule)
                .WithMany(r => r.Transactions)
                .HasForeignKey(t => t.RuleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ---------- Budgets ----------
        builder.Entity<Budget>(e =>
        {
            e.ToTable("Budgets", tb =>
            {
                tb.HasCheckConstraint("ck_budgets_year", "\"BudgetYear\" BETWEEN 2000 AND 2100");
                tb.HasCheckConstraint("ck_budgets_month", "\"BudgetMonth\" BETWEEN 1 AND 12");
                tb.HasCheckConstraint("ck_budgets_limit", "\"LimitAmount\" > 0");
            });
            e.HasKey(b => b.BudgetId);
            e.Property(b => b.LimitAmount).HasPrecision(18, 2);
            e.HasIndex(b => new { b.CategoryId, b.BudgetYear, b.BudgetMonth }).IsUnique();

            e.HasOne(b => b.Category)
                .WithMany(c => c.Budgets)
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
