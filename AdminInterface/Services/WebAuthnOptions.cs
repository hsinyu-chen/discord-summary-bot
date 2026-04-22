using System.Text.Json.Serialization;

namespace AdminInterface.Services;

public class WebAuthnRegistrationOptions
{
    [JsonPropertyName("challenge")]
    public required string Challenge { get; set; }
    
    [JsonPropertyName("rp")]
    public RpOptions Rp { get; set; } = null!;
    
    [JsonPropertyName("user")]
    public UserOptions User { get; set; } = null!;
    
    [JsonPropertyName("pubKeyCredParams")]
    public PubKeyCredParam[] PubKeyCredParams { get; set; } = null!;
    
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; }
    
    [JsonPropertyName("attestation")]
    public string Attestation { get; set; } = null!;
}

public class RpOptions
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("id")]
    public required string Id { get; set; }
}

public class UserOptions
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }
}

public class PubKeyCredParam
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }
    
    [JsonPropertyName("alg")]
    public int Alg { get; set; }
}

public class WebAuthnAuthenticationOptions
{
    [JsonPropertyName("challenge")]
    public required string Challenge { get; set; }
    
    [JsonPropertyName("rpId")]
    public required string RpId { get; set; }
    
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; }
    
    [JsonPropertyName("allowCredentials")]
    public object[] AllowCredentials { get; set; } = null!;
}


