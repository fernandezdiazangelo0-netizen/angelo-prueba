using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace SexShopWASM.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ILocalStorageService _localStorage;
    private readonly HttpClient _http;
    private AuthenticationState _anonymous => new(new ClaimsPrincipal(new ClaimsIdentity()));

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient http)
    {
        _localStorage = localStorage;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            string? token = await _localStorage.GetItemAsStringAsync("authToken");

            if (string.IsNullOrEmpty(token))
            {
                _http.DefaultRequestHeaders.Authorization = null;
                return _anonymous;
            }

            token = token.Trim('"');

            var claims = ParseClaimsFromJwt(token);
            if (claims == null || !claims.Any())
            {
                _http.DefaultRequestHeaders.Authorization = null;
                return _anonymous;
            }

            // Check if token is expired
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim != null && long.TryParse(expClaim.Value, out long exp))
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(exp);
                if (expDate < DateTimeOffset.UtcNow)
                {
                    await _localStorage.RemoveItemAsync("authToken");
                    _http.DefaultRequestHeaders.Authorization = null;
                    return _anonymous;
                }
            }

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth state error: {ex.Message}");
            return _anonymous;
        }
    }

    /// <summary>Call this immediately after a successful login to update UI without a page reload.</summary>
    public void MarkUserAsAuthenticated(string token)
    {
        try
        {
            token = token.Trim('"');
            var claims = ParseClaimsFromJwt(token);
            var identity = (claims != null && claims.Any())
                ? new ClaimsIdentity(claims, "jwt")
                : new ClaimsIdentity();

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MarkUserAsAuthenticated error: {ex.Message}");
        }
    }

    /// <summary>Call this on logout to clear the auth state immediately.</summary>
    public void MarkUserAsLoggedOut()
    {
        _http.DefaultRequestHeaders.Authorization = null;
        NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
    }

    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jwt) || !jwt.Contains('.'))
                return Enumerable.Empty<Claim>();

            var parts = jwt.Split('.');
            if (parts.Length < 2)
                return Enumerable.Empty<Claim>();

            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);

            var claims = new List<Claim>();

            if (keyValuePairs != null)
            {
                foreach (var kvp in keyValuePairs)
                {
                    if (kvp.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in kvp.Value.EnumerateArray())
                        {
                            claims.Add(new Claim(kvp.Key, item.GetString() ?? ""));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(kvp.Key, kvp.Value.ToString()));
                    }
                }
            }

            return claims;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"JWT Parse Error: {ex.Message}");
            return Enumerable.Empty<Claim>();
        }
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        // Replace URL-safe chars
        base64 = base64.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "=";  break;
        }
        return Convert.FromBase64String(base64);
    }
}
