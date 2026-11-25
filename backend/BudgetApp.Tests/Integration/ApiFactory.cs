using BudgetApp.Api.Data;
using BudgetApp.Api.Services;
using BudgetApp.Api.Services.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace BudgetApp.Tests.Integration;

/// <summary>
/// Starts the API against a separate database (budgetapp_test) on the server given in user-secrets.
/// The database is created and migrated automatically, the development data is never touched.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabase = "budgetapp_test";

    public FakeEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        var connection = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Default"))
        {
            Database = TestDatabase
        };

        builder.UseSetting("ConnectionStrings:Default", connection.ConnectionString);
        builder.UseSetting("Jwt:Key", "test-only-key-that-is-long-enough-for-hmac-sha256");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);

            // The tests call the cleanup themselves, it must not run in the background.
            services.Remove(services.Single(s => s.ServiceType == typeof(IHostedService)
                && s.ImplementationType == typeof(UnconfirmedAccountCleanupJob)));
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Safety check: never run the tests against another database.
        Assert.Equal(TestDatabase, db.Database.GetDbConnection().Database);
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();
}
