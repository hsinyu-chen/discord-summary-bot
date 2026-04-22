using SummaryAndCheck.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace AdminInterface.Services;

public class PasskeyService
{
    private readonly SummaryAndCheckDbContext _db;

    public PasskeyService(SummaryAndCheckDbContext db)
    {
        _db = db;
    }

    public async Task<WebAuthnRegistrationOptions> InitRegisterAsync(string username, string displayName)
    {
        var challengeBytes = new byte[32];
        var random = RandomNumberGenerator.Create();
        random.GetBytes(challengeBytes);
        var challenge = Convert.ToBase64String(challengeBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");

        var userBytes = System.Text.Encoding.UTF8.GetBytes(username);
        var userId = Convert.ToBase64String(userBytes);

        // 保存 challenge 用于后续验证
        var storedOptions = new StoredRegistrationOptions
        {
            Challenge = challenge,
            UserName = username,
            CreatedAt = DateTime.UtcNow
        };
        _db.StoredRegistrationOptions.Add(storedOptions);
        await _db.SaveChangesAsync();

        return new WebAuthnRegistrationOptions
        {
            Challenge = challenge,
            Rp = new RpOptions
            {
                Name = "AdminInterface",
                Id = "localhost"
            },
            User = new UserOptions
            {
                Id = userId,
                Name = username,
                DisplayName = displayName
            },
            PubKeyCredParams = new[]
            {
                new PubKeyCredParam { Type = "public-key", Alg = -7 },
                new PubKeyCredParam { Type = "public-key", Alg = -37 }
            },
            Timeout = 60000,
            Attestation = "direct"
        };
    }

    public async Task<bool> CompleteRegisterAsync(string responseJson, string username)
    {
        try
        {
            var options = JsonSerializer.Deserialize<RegistrationResponse>(responseJson);

            if (options == null || string.IsNullOrEmpty(options.Id) || string.IsNullOrEmpty(options.RawId))
            {
                return false;
            }

            // 解析 response 中的 transports
            var transports = string.Empty;
            if (options.Response?.Transports != null)
            {
                transports = string.Join(",", options.Response.Transports);
            }

            // 保存 passkey 到数据库
            var passkey = new PasskeyStorage
            {
                UserName = username,
                CredentialId = options.Id,
                PublicKey = options.RawId, // 这里暂时保存 rawId，实际应保存公钥
                Transport = transports,
                SignCount = 0,
                CreatedAt = DateTime.UtcNow
            };
            _db.PasskeyStorage.Add(passkey);
            await _db.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> InitLoginAsync(string username)
    {
        var challengeBytes = new byte[32];
        var random = RandomNumberGenerator.Create();
        random.GetBytes(challengeBytes);
        var challenge = Convert.ToBase64String(challengeBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");

        // 获取该用户已注册的凭据
        var existingKeys = _db.PasskeyStorage
            .Where(p => p.UserName == username)
            .Select(p => new { p.CredentialId, p.PublicKey })
            .ToList();

        // 构建 allowCredentials 列表
        var allowCredentials = new List<object>();
        foreach (var key in existingKeys)
        {
            allowCredentials.Add(new
            {
                id = key.CredentialId,
                type = "public-key",
                transports = new[] { "internal", "hybrid", "cross-platform" }
            });
        }

        var options = new WebAuthnAuthenticationOptions
        {
            Challenge = challenge,
            RpId = "localhost",
            Timeout = 60000,
            AllowCredentials = allowCredentials.ToArray()
        };

        // 保存 challenge 用于后续验证
        var storedOptions = new StoredAuthenticationOptions
        {
            Challenge = challenge,
            UserName = username,
            CreatedAt = DateTime.UtcNow
        };
        _db.StoredAuthenticationOptions.Add(storedOptions);
        await _db.SaveChangesAsync();

        return JsonSerializer.Serialize(options);
    }

    public async Task<bool> CompleteLoginAsync(string responseJson, string username)
    {
        // 简化版本，实际应验证签名
        return true;
    }

    // WebAuthn 注册响应类
    private class RegistrationResponse
    {
        public string Id { get; set; } = string.Empty;
        public string RawId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public object? Extensions { get; set; }
        public ResponseData? Response { get; set; }
    }

    private class ResponseData
    {
        public string AttestationObject { get; set; } = string.Empty;
        public string ClientDataJSON { get; set; } = string.Empty;
        public string[]? Transports { get; set; }
    }
}