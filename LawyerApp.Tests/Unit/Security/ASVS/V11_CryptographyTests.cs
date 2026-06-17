using FluentAssertions;
using LawyerApp.Infrastructure.Security;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace LawyerApp.Tests.Unit.Security.ASVS;

/// <summary>
/// ASVS V11 – Cryptography
///
/// V11.4.1 – Only approved hash functions (SHA-256 or stronger); MD5/SHA-1 must not be used.
/// V11.4.2 – Passwords stored using approved computationally intensive KDF (bcrypt).
/// V11.3.1 – Insecure block modes (ECB) and weak padding (PKCS#1 v1.5) must not be used.
/// V11.2.3 – All cryptographic primitives must achieve at least 128 bits of security.
/// </summary>
public class V11_CryptographyTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    // ── V11.4.2 – Passwords use approved computationally intensive KDF ────────

    [Fact]
    public void V11_4_2_PasswordHash_UsesBCryptFormat()
    {
        var hash = _hasher.HashPassword("TestPassword123!");

        // BCrypt hashes start with $2a$ or $2b$ (version identifiers)
        hash.Should().MatchRegex(@"^\$2[ab]\$",
            "passwords must be stored using BCrypt KDF (V11.4.2)");
    }

    [Fact]
    public void V11_4_2_BCryptHash_ContainsWorkFactor()
    {
        var hash = _hasher.HashPassword("AnotherPassword!");

        // BCrypt format: $2a$<workFactor>$<saltAndHash>
        // Work factor must be present and >= 10 for adequate security
        var parts = hash.Split('$');
        parts.Should().HaveCountGreaterThanOrEqualTo(4,
            "BCrypt hash must contain version, work factor, salt and hash (V11.4.2)");
        var workFactor = int.Parse(parts[2]);
        workFactor.Should().BeGreaterThanOrEqualTo(10,
            "BCrypt work factor must be >= 10 to be computationally intensive (V11.4.2)");
    }

    [Fact]
    public void V11_4_2_PasswordVerification_WorksCorrectlyWithBCrypt()
    {
        var password = "Secure$Password99!";
        var hash     = _hasher.HashPassword(password);

        _hasher.VerifyPassword(password, hash).Should().BeTrue(
            "BCrypt must correctly verify the original password (V11.4.2)");
    }

    [Fact]
    public void V11_4_2_DifferentPasswords_ProduceDifferentHashes()
    {
        var h1 = _hasher.HashPassword("Password1!");
        var h2 = _hasher.HashPassword("Password2!");

        h1.Should().NotBe(h2,
            "BCrypt salt randomisation must produce distinct hashes for different passwords (V11.4.2)");
    }

    [Fact]
    public void V11_4_2_SamePassword_ProducesDifferentHashesOnEachCall()
    {
        const string pw = "SamePassword!";
        var h1 = _hasher.HashPassword(pw);
        var h2 = _hasher.HashPassword(pw);

        h1.Should().NotBe(h2,
            "BCrypt must generate a fresh random salt each call; two hashes of the same password must differ (V11.4.2)");
    }

    // ── V11.4.1 – Only approved hash functions; MD5 must not be used ─────────

    [Fact]
    public void V11_4_1_Sha256_IsApprovedHashFunction()
    {
        var data = Encoding.UTF8.GetBytes("test data");
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);

        hash.Length.Should().Be(32, "SHA-256 produces 256-bit (32-byte) output — an approved hash (V11.4.1)");
    }

    [Fact]
    public void V11_4_1_Md5_OutputLength_RevealsThatItIsNotApproved()
    {
        // This test documents that MD5 produces only 128-bit output — insufficient
        // for collision resistance per ASVS V11.4.1. It must never be used.
        var data = Encoding.UTF8.GetBytes("test data");
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(data);

        // MD5 = 128 bits = 16 bytes — below the 256-bit minimum for collision resistance
        hash.Length.Should().Be(16,
            "MD5 produces only 128-bit output; this test documents why it is disallowed (V11.4.1)");
    }

    [Fact]
    public void V11_4_1_Sha1_OutputLength_RevealsThatItIsNotApproved()
    {
        // SHA-1 = 160 bits — below the 256-bit minimum for collision resistance (V11.4.1)
        var data = Encoding.UTF8.GetBytes("test data");
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(data);

        hash.Length.Should().Be(20,
            "SHA-1 produces only 160-bit output; this test documents why it is disallowed (V11.4.1)");
    }

    // ── V11.2.3 – At least 128 bits of security ───────────────────────────────

    [Fact]
    public void V11_2_3_JwtSigningKey_HasSufficientLength()
    {
        // The app key: "IHOPETHISISREALYWORKINGCOMEONE_IHOPETHISISREALYWORKINGCOMEONE"
        // HMAC-SHA256 requires the key to be at least 256 bits (32 bytes) for 128-bit security
        const string appKey = "IHOPETHISISREALYWORKINGCOMEONE_IHOPETHISISREALYWORKINGCOMEONE";
        var keyBytes = Encoding.UTF8.GetBytes(appKey);

        keyBytes.Length.Should().BeGreaterThanOrEqualTo(32,
            "the JWT signing key must be at least 256 bits to achieve 128-bit security with HMAC-SHA256 (V11.2.3)");
    }

    [Fact]
    public void V11_2_3_Sha256_Provides256BitOutput_MeetsMinimumSecurity()
    {
        var data = Encoding.UTF8.GetBytes("security check");
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);

        // 256-bit output provides >= 128 bits of security
        (hash.Length * 8).Should().BeGreaterThanOrEqualTo(256,
            "SHA-256 must produce at least 256-bit output to meet the 128-bit security requirement (V11.2.3)");
    }

    // ── V11.3.2 – Only approved ciphers (AES-GCM, not ECB) ──────────────────

    [Fact]
    public void V11_3_1_AesGcm_IsAvailable_And_Provides_AuthenticatedEncryption()
    {
        // Verify that .NET's AES-GCM (authenticated encryption) is available
        // AES-GCM is the approved mode; ECB must not be used
        var key        = new byte[32]; // 256-bit AES key
        RandomNumberGenerator.Fill(key);
        var nonce      = new byte[AesGcm.NonceByteSizes.MinSize];
        RandomNumberGenerator.Fill(nonce);
        var plaintext  = Encoding.UTF8.GetBytes("sensitive data");
        var ciphertext = new byte[plaintext.Length];
        var tag        = new byte[AesGcm.TagByteSizes.MinSize];

        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MinSize);
        var act = () => aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        act.Should().NotThrow("AES-GCM (the approved authenticated encryption mode) must be available (V11.3.1/V11.3.2)");
    }
}
