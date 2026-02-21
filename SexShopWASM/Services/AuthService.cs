using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using SexShopWASM.Auth;
using SexShopWASM.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace SexShopWASM.Services;

public interface IAuthService
{
    Task<AuthResponse?> Login(LoginModel loginModel);
    Task<RegisterResult> Register(RegisterModel registerModel);
    Task Logout();
}

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly CustomAuthStateProvider _authStateProvider;
    private readonly ILocalStorageService _localStorage;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthService(HttpClient httpClient,
                       AuthenticationStateProvider authenticationStateProvider,
                       ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authStateProvider = (CustomAuthStateProvider)authenticationStateProvider;
        _localStorage = localStorage;
    }

    public async Task<AuthResponse?> Login(LoginModel loginModel)
    {
        try
        {
            var result = await _httpClient.PostAsJsonAsync("api/auth/login", loginModel);

            if (result.IsSuccessStatusCode)
            {
                var json = await result.Content.ReadAsStringAsync();
                var response = JsonSerializer.Deserialize<AuthResponse>(json, _jsonOptions);

                if (response != null && !string.IsNullOrEmpty(response.Token))
                {
                    await _localStorage.SetItemAsStringAsync("authToken", response.Token);
                    _authStateProvider.MarkUserAsAuthenticated(response.Token);
                    return response;
                }
            }

            Console.WriteLine($"Login failed: {result.StatusCode}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login exception: {ex.Message}");
            return null;
        }
    }

    public async Task<RegisterResult> Register(RegisterModel registerModel)
    {
        try
        {
            var result = await _httpClient.PostAsJsonAsync("api/auth/register", registerModel);
            if (result.IsSuccessStatusCode)
            {
                return new RegisterResult { Successful = true };
            }

            var errorContent = await result.Content.ReadAsStringAsync();
            return new RegisterResult
            {
                Successful = false,
                Errors = new[] { $"Registration failed: {result.ReasonPhrase}. {errorContent}" }
            };
        }
        catch (Exception ex)
        {
            return new RegisterResult
            {
                Successful = false,
                Errors = new[] { $"Connection error: {ex.Message}" }
            };
        }
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        _authStateProvider.MarkUserAsLoggedOut();
    }
}
