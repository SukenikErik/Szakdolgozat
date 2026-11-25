using System.ComponentModel.DataAnnotations;

namespace BudgetApp.Api.Services;

/// <summary>The "Jwt" configuration section. The key comes from user-secrets, never from appsettings.</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 needs a key of at least 256 bits (32 bytes).
    [Required, MinLength(32)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryMinutes { get; set; } = 60;
}
