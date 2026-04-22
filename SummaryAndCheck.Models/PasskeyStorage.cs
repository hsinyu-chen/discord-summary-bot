namespace SummaryAndCheck.Models;

public class PasskeyStorage
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string CredentialId { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string Transport { get; set; } = string.Empty;
    public int SignCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class StoredRegistrationOptions
{
    public int Id { get; set; }
    public string Challenge { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class StoredAuthenticationOptions
{
    public int Id { get; set; }
    public string Challenge { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}