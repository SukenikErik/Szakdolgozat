using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using BudgetApp.Api.Data;
using BudgetApp.Api.Dtos;
using BudgetApp.Api.Entities;
using BudgetApp.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetApp.Tests.Integration;

public class EmailConfirmationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact] // F-06, F-07
    public async Task Register_SendsEmailWithLinkAndDeletionNotice()
    {
        var auth = await RegisterAsync();

        var email = Assert.Single(factory.Emails.SentTo(auth.User.Email));
        Assert.Contains("/confirm-email?userId=" + auth.User.UserId, email.TextBody);
        Assert.Contains("permanently deleted", email.TextBody);
        Assert.Contains("permanently deleted", email.HtmlBody);
        Assert.False(auth.User.EmailConfirmed);
        Assert.Equal(30, auth.User.ConfirmationDaysLeft);
    }

    [Fact] // F-06
    public async Task Register_EncodesTheNameInTheHtmlEmail()
    {
        var auth = await RegisterAsync(displayName: "<b>Bob</b>");

        var email = Assert.Single(factory.Emails.SentTo(auth.User.Email));
        Assert.DoesNotContain("<b>Bob", email.HtmlBody);
    }

    [Fact] // F-06
    public async Task Register_SucceedsWhenTheEmailCannotBeSent()
    {
        var auth = await RegisterAsync(prefix: "undeliverable");

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "me", auth.AccessToken)).StatusCode);
        // A failed sending must not start the one minute waiting time.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await ResendAsync(auth)).StatusCode);
    }

    [Fact] // F-06
    public async Task Confirm_WithTheLinkFromTheEmail_ConfirmsTheAccount()
    {
        var auth = await RegisterAsync();
        var token = TokenFromEmail(auth.User.Email);

        Assert.Equal(HttpStatusCode.NoContent, (await ConfirmAsync(auth.User.UserId, token)).StatusCode);

        var response = await SendAsync(HttpMethod.Get, "me", auth.AccessToken);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.True(user!.EmailConfirmed);
        Assert.Null(user.ConfirmationDaysLeft);
        // Opening the link a second time is not an error.
        Assert.Equal(HttpStatusCode.NoContent, (await ConfirmAsync(auth.User.UserId, token)).StatusCode);
    }

    [Fact] // F-06
    public async Task Confirm_WithWrongToken_ReturnsBadRequest()
    {
        var auth = await RegisterAsync();
        var other = await RegisterAsync();
        var otherToken = TokenFromEmail(other.User.Email);

        Assert.Equal(HttpStatusCode.BadRequest, (await ConfirmAsync(auth.User.UserId, "wrong")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ConfirmAsync(auth.User.UserId, "***")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ConfirmAsync(auth.User.UserId, otherToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ConfirmAsync(Guid.NewGuid(), otherToken)).StatusCode);
    }

    [Fact] // N-12
    public async Task Resend_RightAfterRegistration_ReturnsTooManyRequests()
    {
        var auth = await RegisterAsync();

        Assert.Equal(HttpStatusCode.TooManyRequests, (await ResendAsync(auth)).StatusCode);
        Assert.Single(factory.Emails.SentTo(auth.User.Email));
    }

    [Fact] // F-06
    public async Task Resend_WhenConfirmed_SendsNothing()
    {
        var auth = await RegisterAsync();
        await ConfirmAsync(auth.User.UserId, TokenFromEmail(auth.User.Email));

        Assert.Equal(HttpStatusCode.NoContent, (await ResendAsync(auth)).StatusCode);
        Assert.Single(factory.Emails.SentTo(auth.User.Email));
    }

    [Fact] // F-06
    public async Task Resend_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/auth/resend-confirmation", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact] // F-07, F-05 (cascading delete)
    public async Task Cleanup_DeletesOnlyExpiredUnconfirmedAccounts_WithAllTheirData()
    {
        var expired = (await RegisterAsync()).User.UserId;
        var expiredConfirmed = (await RegisterAsync()).User.UserId;
        var notExpired = (await RegisterAsync()).User.UserId;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        await SetAccountAsync(db, expired, now.AddDays(-30).AddMinutes(-5), confirmed: false);
        await SetAccountAsync(db, expiredConfirmed, now.AddDays(-400), confirmed: true);
        await SetAccountAsync(db, notExpired, now.AddDays(-30).AddMinutes(5), confirmed: false);
        await AddDataAsync(db, expired);

        await scope.ServiceProvider.GetRequiredService<UnconfirmedAccountCleanup>().DeleteExpiredAsync();

        Guid[] ids = [expired, expiredConfirmed, notExpired];
        var remaining = await db.Users.Where(u => ids.Contains(u.Id)).Select(u => u.Id).ToListAsync();
        Assert.Equal(new[] { expiredConfirmed, notExpired }.Order(), remaining.Order());
        Assert.False(await db.Categories.AnyAsync(c => c.UserId == expired));
        Assert.False(await db.Transactions.AnyAsync(t => t.UserId == expired));
        Assert.False(await db.RecurringRules.AnyAsync(r => r.UserId == expired));
        Assert.False(await db.Goals.AnyAsync(g => g.UserId == expired));
    }

    [Theory] // F-08
    [InlineData(0, 30)]
    [InlineData(1, 29)]
    [InlineData(29.5, 1)]
    [InlineData(31, 0)]
    public void DaysLeft_CountsStartedDays(double accountAgeDays, int expected)
    {
        var now = new DateTime(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);
        var user = new ApplicationUser { CreatedAt = now.AddDays(-accountAgeDays) };

        Assert.Equal(expected, UnconfirmedAccountPolicy.DaysLeft(user, now));
    }

    private static Task SetAccountAsync(AppDbContext db, Guid userId, DateTime createdAt, bool confirmed) =>
        db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(setters => setters
            .SetProperty(u => u.CreatedAt, createdAt)
            .SetProperty(u => u.EmailConfirmed, confirmed));

    private static async Task AddDataAsync(AppDbContext db, Guid userId)
    {
        var category = await db.Categories.FirstAsync(c => c.UserId == userId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var rule = new RecurringRule
        {
            UserId = userId, Category = category, Name = "Rent", Amount = 100, StartDate = today
        };
        var goal = new Goal { UserId = userId, Name = "Bike", TargetAmount = 1000 };
        db.AddRange(
            rule,
            goal,
            new Budget { Category = category, BudgetYear = 2026, BudgetMonth = 11, LimitAmount = 500 },
            new Transaction
            {
                UserId = userId, TransactionType = TransactionType.FixedExpense, Amount = 100,
                TransactionDate = today, Category = category, Rule = rule
            },
            new Transaction
            {
                UserId = userId, TransactionType = TransactionType.Saving, Amount = 50,
                TransactionDate = today, Category = category, Goal = goal
            });
        await db.SaveChangesAsync();
    }

    private async Task<AuthResponse> RegisterAsync(string prefix = "user", string displayName = "Test User")
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"{prefix}-{Guid.NewGuid():N}@example.com",
            password = "Passw0rd123",
            displayName
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private string TokenFromEmail(string email) =>
        Regex.Match(Assert.Single(factory.Emails.SentTo(email)).TextBody, @"token=(\S+)").Groups[1].Value;

    private Task<HttpResponseMessage> ConfirmAsync(Guid userId, string token) =>
        _client.PostAsJsonAsync("/api/auth/confirm-email", new { userId, token });

    private Task<HttpResponseMessage> ResendAsync(AuthResponse auth) =>
        SendAsync(HttpMethod.Post, "resend-confirmation", auth.AccessToken);

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, "/api/auth/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }
}
