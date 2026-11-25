using BudgetApp.Api.Dtos;
using BudgetApp.Api.Extensions;
using BudgetApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BudgetApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService authService, EmailConfirmationService emailConfirmation) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var (result, response) = await authService.RegisterAsync(request);
        if (response is not null)
        {
            return CreatedAtAction(nameof(Me), response);
        }

        if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail)))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: "This email address is already registered.");
        }

        foreach (var error in result.Errors)
        {
            var field = error.Code.StartsWith("Password") ? nameof(request.Password) : nameof(request.Email);
            ModelState.AddModelError(field, error.Description);
        }

        return ValidationProblem(ModelState);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var response = await authService.LoginAsync(request);

        // The same message in every case, so it does not reveal which emails are registered.
        return response is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Invalid email or password.")
            : response;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> Me()
    {
        // The token may outlive a deleted account.
        var user = await authService.GetUserAsync(User.GetUserId());
        return user is null ? Unauthorized() : user;
    }

    // Anonymous, because the link may be opened on another device. The token proves the ownership of the email.
    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        return await emailConfirmation.ConfirmAsync(request.UserId, request.Token)
            ? NoContent()
            : Problem(statusCode: StatusCodes.Status400BadRequest, detail: "This confirmation link is invalid or has expired.");
    }

    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation()
    {
        return await authService.ResendConfirmationAsync(User.GetUserId()) switch
        {
            null => Unauthorized(),
            SendConfirmationResult.TooSoon => Problem(
                statusCode: StatusCodes.Status429TooManyRequests,
                detail: "Please wait a minute before requesting another email."),
            SendConfirmationResult.Failed => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: "The email could not be sent. Please try again later."),
            _ => NoContent()
        };
    }
}
