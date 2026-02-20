using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using SexShopWASM.Auth;
using SexShopWASM.Models;
using System.Net.Http.Json;

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
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly ILocalStorageService _localStorage;

    public AuthService(HttpClient httpClient,
                       AuthenticationStateProvider authenticationStateProvider,
                       ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _authenticationStateProvider = authenticationStateProvider;
        _localStorage = localStorage;
    }

    public async Task<AuthResponse?> Login(LoginModel loginModel)
    {
        var result = await _httpClient.PostAsJsonAsync("api/auth/login", loginModel);

        if (result.IsSuccessStatusCode)
        {
            var response = await result.Content.ReadFromJsonAsync<AuthResponse>();
            await _localStorage.SetItemAsync("authToken", response!.Token);
            await ((CustomAuthStateProvider)_authenticationStateProvider).GetAuthenticationStateAsync(); // Trigger state change
            return response;
        }

        return null;
    }

    public async Task<RegisterResult> Register(RegisterModel registerModel)
    {
        var result = await _httpClient.PostAsJsonAsync("api/auth/register", registerModel);
        if (result.IsSuccessStatusCode)
        {
            return new RegisterResult { Successful = true };
        }
        
        // Try to read error details if available, otherwise generic error
        var errorContent = await result.Content.ReadAsStringAsync();
        return new RegisterResult { Successful = false, Errors = new[] { $"Registration failed: {result.ReasonPhrase} - {errorContent}" } };
    }

    public async Task Logout()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await ((CustomAuthStateProvider)_authenticationStateProvider).GetAuthenticationStateAsync();
    }
}
