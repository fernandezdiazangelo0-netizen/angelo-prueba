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

    public CustomAuthStateProvider(ILocalStorageService localStorage, HttpClient http)
    {
        _localStorage = localStorage;
        _http = http;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity();
        _http.DefaultRequestHeaders.Authorization = null;

        try
        {
            string token = await _localStorage.GetItemAsStringAsync("authToken");
            if (!string.IsNullOrEmpty(token))
            {
                // Remove quotes if present
                token = token.Trim('"');
                
                var claims = ParseClaimsFromJwt(token);
                if (claims != null && claims.Any())
                {
                    identity = new ClaimsIdentity(claims, "jwt");
                    _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth state error: {ex.Message}");
            try 
            {
                await _localStorage.RemoveItemAsync("authToken");
            }
            catch { /* Ignore storage removal errors */ }
        }

        var user = new ClaimsPrincipal(identity);
        var state = new AuthenticationState(user);

        NotifyAuthenticationStateChanged(Task.FromResult(state));

        return state;
    }

    public static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jwt) || !jwt.Contains('.')) return Enumerable.Empty<Claim>();

            var parts = jwt.Split('.');
            if (parts.Length < 2) return Enumerable.Empty<Claim>();

            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            var claims = new List<Claim>();

            if (keyValuePairs != null)
            {
                if (keyValuePairs.TryGetValue(ClaimTypes.Role, out object? roles) && roles != null)
                {
                    var rolesStr = roles.ToString();
                    if (!string.IsNullOrEmpty(rolesStr))
                    {
                        if (rolesStr.Trim().StartsWith("["))
                        {
                            var parsedRoles = JsonSerializer.Deserialize<string[]>(rolesStr);
                            if (parsedRoles != null)
                            {
                                claims.AddRange(parsedRoles.Select(r => new Claim(ClaimTypes.Role, r)));
                            }
                        }
                        else
                        {
                            claims.Add(new Claim(ClaimTypes.Role, rolesStr));
                        }
                    }
                    keyValuePairs.Remove(ClaimTypes.Role);
                }

                claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? "")));
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
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
