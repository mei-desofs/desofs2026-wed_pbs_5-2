using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using LawyerApp.Domain.Aggregates.UserAggregate;
using LawyerApp.Domain.Shared;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LawyerApp.Tests.Unit.Security.ASVS;

/// <summary>
/// ASVS V9 – Self-contained Tokens
///
/// V9.1.1 – Tokens are validated using digital signature / MAC before accepting contents.
/// V9.1.2 – Only allowlisted algorithms can be used; 'None' algorithm must be rejected.
/// V9.1.3 – Key material is from pre-configured trusted sources only.
/// V9.2.1 – exp claim is present and enforced.
/// V9.2.3 – aud claim is validated against an allowlist.
/// </summary>
public class V9_SelfContainedTokenTests
{
    private const string SecretKey  = "IHOPETHISISREALYWORKINGCOMEONE_IHOPETHISISREALYWORKINGCOMEONE";
    private const string Issuer     = "LawyerApp";
    private const string Audience   = "LawyerAppUsers";

    private static string MakeToken(
        string secret    = SecretKey,
        string issuer    = Issuer,
        string audience  = Audience,
        string algorithm = SecurityAlgorithms.HmacSha256,
        DateTime? expires = null,
        string role      = "Client")
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, "test@test.com"),
            new Claim(ClaimTypes.Role,               role),
        };
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, algorithm);
        var token = new JwtSecurityToken(issuer, audience, claims,
            expires: expires ?? DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static TokenValidationParameters BuildValidationParams(string? audience = null) =>
        new()
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = Issuer,
            ValidAudience            = audience ?? Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
            ClockSkew                = TimeSpan.Zero,
        };

    // ── V9.1.1 – Signature integrity ─────────────────────────────────────────

    [Fact]
    public void V9_1_1_ValidToken_PassesSignatureValidation()
    {
        var token   = MakeToken();
        var handler = new JwtSecurityTokenHandler();

        var act = () => handler.ValidateToken(token, BuildValidationParams(), out _);

        act.Should().NotThrow("a token signed with the correct key must be accepted");
    }

    [Fact]
    public void V9_1_1_TamperedToken_FailsSignatureValidation()
    {
        var token  = MakeToken();
        var parts  = token.Split('.');
        // Flip the last character of the signature segment to corrupt it
        var sig    = parts[2];
        parts[2]   = sig[..^1] + (sig[^1] == 'A' ? 'B' : 'A');
        var tampered = string.Join('.', parts);

        var handler = new JwtSecurityTokenHandler();
        var act     = () => handler.ValidateToken(tampered, BuildValidationParams(), out _);

        act.Should().Throw<SecurityTokenInvalidSignatureException>(
            "a tampered signature must be rejected (V9.1.1)");
    }

    [Fact]
    public void V9_1_1_TokenSignedWithDifferentKey_IsRejected()
    {
        var wrongKey   = "ADIFFERENTSECRETKEYTHATISNOTVALID!";
        var tokenWrong = MakeToken(secret: wrongKey);
        var handler    = new JwtSecurityTokenHandler();

        var act = () => handler.ValidateToken(tokenWrong, BuildValidationParams(), out _);

        act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>(
            "a token signed with an untrusted key must be rejected (V9.1.1 / V9.1.3)");
    }

    // ── V9.1.2 – Algorithm allowlist; 'None' must be rejected ────────────────

    [Fact]
    public void V9_1_2_HmacSha256_IsAcceptedAlgorithm()
    {
        var token   = MakeToken(algorithm: SecurityAlgorithms.HmacSha256);
        var handler = new JwtSecurityTokenHandler();
        var parsed  = handler.ReadJwtToken(token);

        parsed.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256,
            "HmacSha256 must be used (V9.1.2)");
    }

    [Fact]
    public void V9_1_2_NoneAlgorithm_IsRejectedByValidationParameters()
    {
        // Build an unsigned token manually (alg=none)
        var header  = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"none\",\"typ\":\"JWT\"}"))
                             .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                             $"{{\"sub\":\"{Guid.NewGuid()}\",\"exp\":{DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds()}}}"))
                             .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var noneToken = $"{header}.{payload}.";

        var handler = new JwtSecurityTokenHandler();
        var act     = () => handler.ValidateToken(noneToken, BuildValidationParams(), out _);

        // The validation params require a signing key; none-alg tokens have no signature
        act.Should().Throw<Exception>("'none' algorithm tokens must be rejected (V9.1.2)");
    }

    // ── V9.2.1 – exp claim is present and enforced ───────────────────────────

    [Fact]
    public void V9_2_1_Token_ContainsExpClaim()
    {
        var token  = MakeToken();
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.ValidTo.Should().BeAfter(DateTime.MinValue, "exp claim must be present (V9.2.1)");
    }

    [Fact]
    public void V9_2_1_ExpiredToken_IsRejectedByValidator()
    {
        var expired = MakeToken(expires: DateTime.UtcNow.AddMinutes(-1));
        var handler = new JwtSecurityTokenHandler();

        var act = () => handler.ValidateToken(expired, BuildValidationParams(), out _);

        act.Should().Throw<SecurityTokenExpiredException>(
            "an expired token must be rejected (V9.2.1)");
    }

    [Fact]
    public void V9_2_1_TokenWithFutureExp_IsAccepted()
    {
        var valid = MakeToken(expires: DateTime.UtcNow.AddMinutes(30));
        var handler = new JwtSecurityTokenHandler();

        var act = () => handler.ValidateToken(valid, BuildValidationParams(), out _);

        act.Should().NotThrow("a token with a future exp must be accepted (V9.2.1)");
    }

    // ── V9.2.3 – aud claim validated against allowlist ───────────────────────

    [Fact]
    public void V9_2_3_Token_ContainsAudienceClaim()
    {
        var token  = MakeToken();
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Audiences.Should().Contain(Audience,
            "aud claim must identify the intended audience (V9.2.3)");
    }

    [Fact]
    public void V9_2_3_TokenWithWrongAudience_IsRejected()
    {
        var token   = MakeToken(audience: "SomeOtherService");
        var handler = new JwtSecurityTokenHandler();

        var act = () => handler.ValidateToken(token, BuildValidationParams(), out _);

        act.Should().Throw<SecurityTokenInvalidAudienceException>(
            "a token intended for a different audience must be rejected (V9.2.3)");
    }

    [Fact]
    public void V9_2_3_TokenWithCorrectAudience_IsAccepted()
    {
        var token   = MakeToken(audience: Audience);
        var handler = new JwtSecurityTokenHandler();

        var act = () => handler.ValidateToken(token, BuildValidationParams(Audience), out _);

        act.Should().NotThrow("a token with the correct audience must be accepted (V9.2.3)");
    }

    // ── V9.1.3 – Issuer validated against trusted pre-configured source ───────

    [Fact]
    public void V9_1_3_TokenWithWrongIssuer_IsRejected()
    {
        var token   = MakeToken(issuer: "MaliciousIssuer");
        var handler = new JwtSecurityTokenHandler();

        var act = () => handler.ValidateToken(token, BuildValidationParams(), out _);

        act.Should().Throw<SecurityTokenInvalidIssuerException>(
            "tokens from untrusted issuers must be rejected (V9.1.3)");
    }

    [Fact]
    public void V9_1_3_TokenWithCorrectIssuer_IsAccepted()
    {
        var token   = MakeToken(issuer: Issuer);
        var handler = new JwtSecurityTokenHandler();

        var act = () => handler.ValidateToken(token, BuildValidationParams(), out _);

        act.Should().NotThrow("a token from the trusted issuer must be accepted (V9.1.3)");
    }
}
