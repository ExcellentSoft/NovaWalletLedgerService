using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NovaWallet.Application.Dtos;

namespace NovaWallet.API.Auth;

/// <summary>
/// Mock token endpoint standing in for a real identity provider. Given any subject/customer id,
/// it issues a signed JWT so callers can exercise the protected wallet endpoints. In a production
/// system this would be replaced by a real OAuth2/OIDC issuer (e.g., Azure AD, Auth0, Keycloak).
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    /// <summary>Issues a mock bearer token for the given subject, to be used as Authorization: Bearer &lt;token&gt;.</summary>
    [HttpPost("token")]
    public IActionResult IssueToken([FromBody] TokenRequest request)
    {
        var subject = string.IsNullOrWhiteSpace(request.Subject) ? Guid.NewGuid().ToString() : request.Subject;
        var newCustomerId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim(JwtRegisteredClaimNames.Jti, newCustomerId.ToString()),
            new Claim(ClaimTypes.Name, subject),
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
            signingCredentials: creds);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        // Assuming the subject is a valid GUID for customer ID
        return Ok(new TokenResponse(tokenString, "Bearer", _jwtOptions.ExpiryMinutes * 60, newCustomerId));
    }
}

