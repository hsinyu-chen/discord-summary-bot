using Microsoft.AspNetCore.Components.Authorization;
using SummaryAndCheck.Models;

namespace AdminInterface.Services;

public class AuthService
{
    private readonly SummaryAndCheckDbContext _db;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly PasskeyService _passkeyService;

    public AuthService(SummaryAndCheckDbContext db, AuthenticationStateProvider authStateProvider, PasskeyService passkeyService)
    {
        _db = db;
        _authStateProvider = authStateProvider;
        _passkeyService = passkeyService;
    }

    public async Task<string> InitLoginAsync(string username)
    {
        var options = await _passkeyService.InitLoginAsync(username);
        return options; // Return JSON string for login
    }

    public async Task<bool> CompleteLoginAsync(string responseJson, string username)
    {
        var success = await _passkeyService.CompleteLoginAsync(responseJson, username);
        if (success)
        {
            ((CustomAuthenticationStateProvider)_authStateProvider).MarkUserAsAuthenticated(username);
        }
        return success;
    }

    public async Task<WebAuthnRegistrationOptions> InitRegisterAsync(string username, string displayName)
    {
        return await _passkeyService.InitRegisterAsync(username, displayName);
    }

    public async Task<bool> CompleteRegisterAsync(string responseJson, string username)
    {
        return await _passkeyService.CompleteRegisterAsync(responseJson, username);
    }
}
