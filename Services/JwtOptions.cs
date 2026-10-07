namespace JajkoBackend.Services;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; set; }
    public required string Audience { get; set; }

    // Klucz HMAC-SHA256, minimum 32 bajty
    public required string Key { get; set; }

    public int ExpiresInMinutes { get; set; } = 60;
}
