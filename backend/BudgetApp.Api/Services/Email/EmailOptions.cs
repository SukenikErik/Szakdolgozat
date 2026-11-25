using System.ComponentModel.DataAnnotations;

namespace BudgetApp.Api.Services.Email;

/// <summary>The "Email" configuration section. User name and password belong in user-secrets.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; }

    /// <summary>True for real providers, false only for the local smtp4dev server.</summary>
    public bool UseTls { get; set; } = true;

    // Empty for smtp4dev, which needs no login.
    public string? UserName { get; set; }
    public string? Password { get; set; }

    [Required, EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    [Required]
    public string FromName { get; set; } = string.Empty;

    /// <summary>The address of the web client, the links in the emails point here.</summary>
    [Required, Url]
    public string WebUrl { get; set; } = string.Empty;
}
