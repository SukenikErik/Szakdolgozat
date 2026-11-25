using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BudgetApp.Api.Data;
using BudgetApp.Api.Dtos;
using BudgetApp.Api.Entities;
using BudgetApp.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BudgetApp.Tests.Integration;

public class AuthTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "Passw0rd123";
    private const string WrongPassword = "Wrong12345";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact] // F-01
    public async Task Register_ReturnsUsableToken()
    {
        var email = NewEmail();
        var response = await RegisterAsync(email);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await ReadAuthAsync(response);
        var me = await (await MeAsync(auth.AccessToken)).Content.ReadFromJsonAsync<UserResponse>();

        Assert.Equal(email, me!.Email);
        Assert.Equal("Test User", me.DisplayName);
    }

    [Fact] // F-04, N-01
    public async Task Register_CreatesDefaultCategories_AndHashesPassword()
    {
        var auth = await ReadAuthAsync(await RegisterAsync(NewEmail()));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.Include(u => u.Categories).SingleAsync(u => u.Id == auth.User.UserId);

        Assert.Equal(
            DefaultCategories.Create().Select(c => c.Name).Order(),
            user.Categories.Select(c => c.Name).Order());
        Assert.NotNull(user.PasswordHash);
        Assert.DoesNotContain(Password, user.PasswordHash);
    }

    [Fact] // F-01
    public async Task Register_DuplicateEmail_ReturnsConflict_CaseInsensitively()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var response = await RegisterAsync(email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory] // N-04
    [InlineData("not-an-email", Password, "Email")]
    [InlineData("", Password, "Email")]
    [InlineData("weak@example.com", "short", "Password")]
    [InlineData("weak@example.com", "", "Password")]
    public async Task Register_InvalidInput_ReturnsFieldError(string email, string password, string field)
    {
        var response = await RegisterAsync(email, password);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(field, problem!.Errors.Keys);
    }

    [Fact] // F-02
    public async Task Login_ReturnsUsableToken()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var response = await LoginAsync(email, Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await ReadAuthAsync(response);
        Assert.Equal(HttpStatusCode.OK, (await MeAsync(auth.AccessToken)).StatusCode);
    }

    [Fact] // F-02
    public async Task Login_WrongPasswordOrUnknownEmail_ReturnsUnauthorized()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, WrongPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(NewEmail(), Password)).StatusCode);
    }

    [Fact] // F-02
    public async Task Login_AfterFiveFailures_IsLockedOut()
    {
        var email = NewEmail();
        await RegisterAsync(email);
        for (var i = 0; i < 5; i++)
        {
            await LoginAsync(email, WrongPassword);
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, Password)).StatusCode);
    }

    [Fact] // F-03
    public async Task Me_WithoutOrWithInvalidToken_ReturnsUnauthorized()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await MeAsync("invalid.token.value")).StatusCode);
    }

    [Fact] // N-02
    public async Task Me_WithTokenSignedByAnotherKey_ReturnsUnauthorized()
    {
        var token = await CreateTokenAsync("another-key-that-is-long-enough-for-hmac-sha256", DateTimeOffset.UtcNow);

        Assert.Equal(HttpStatusCode.Unauthorized, (await MeAsync(token)).StatusCode);
    }

    [Theory] // N-02
    [InlineData(0, HttpStatusCode.OK)]
    [InlineData(-2, HttpStatusCode.Unauthorized)] // the token is valid for one hour
    public async Task Me_AcceptsTokenOnlyUntilItExpires(int issuedHoursFromNow, HttpStatusCode expected)
    {
        var key = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value.Key;
        var token = await CreateTokenAsync(key, DateTimeOffset.UtcNow.AddHours(issuedHoursFromNow));

        Assert.Equal(expected, (await MeAsync(token)).StatusCode);
    }

    [Fact] // F-03
    public async Task Me_WhenUserWasDeleted_ReturnsUnauthorized()
    {
        var auth = await ReadAuthAsync(await RegisterAsync(NewEmail()));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Users.Where(u => u.Id == auth.User.UserId).ExecuteDeleteAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await MeAsync(auth.AccessToken)).StatusCode);
    }

    // Unique per test, so the tests do not depend on each other's data.
    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private Task<HttpResponseMessage> RegisterAsync(string email, string password = Password) =>
        _client.PostAsJsonAsync("/api/auth/register", new { email, password, displayName = "Test User" });

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/auth/login", new { email, password });

    private Task<HttpResponseMessage> MeAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }

    private static async Task<AuthResponse> ReadAuthAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<AuthResponse>())!;

    // Creates a token for an existing user, so only the key or the issue time can cause a rejection.
    private async Task<string> CreateTokenAsync(string key, DateTimeOffset issuedAt)
    {
        var auth = await ReadAuthAsync(await RegisterAsync(NewEmail()));
        var current = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var options = new JwtOptions
        {
            Key = key,
            Issuer = current.Issuer,
            Audience = current.Audience,
            ExpiryMinutes = 60
        };

        return new TokenService(Options.Create(options), new FixedTimeProvider(issuedAt))
            .CreateToken(new ApplicationUser { Id = auth.User.UserId }).Token;
    }

    private class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
