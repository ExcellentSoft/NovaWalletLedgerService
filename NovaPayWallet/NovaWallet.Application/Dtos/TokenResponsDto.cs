using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NovaWallet.Application.Dtos
{
    
    /// <summary>Request body for issuing a mock token. Subject typically represents a customer id.</summary>
    public record TokenRequest(string? Subject);

    /// <summary>Bearer token response.</summary>
    public record TokenResponse(string AccessToken, string TokenType, int ExpiresInSeconds);

}
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "NovaWallet.MockIssuer";
    public string Audience { get; set; } = "NovaWallet.API";

    /// <summary>Symmetric signing key. Must be at least 32 characters for HMAC-SHA256.</summary>
    public string SigningKey { get; set; } = "novawallet-dev-signing-key-change-me-32chars";

    public int ExpiryMinutes { get; set; } = 60;
}
