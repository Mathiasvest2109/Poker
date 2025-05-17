using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;
using System.Threading.Tasks;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
    private readonly IJSRuntime _jsRuntime;

    public CustomAuthStateProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            var username = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "username");

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username))
            {
                var identity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username)
                }, "Custom");
                _currentUser = new ClaimsPrincipal(identity);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving auth state: {ex.Message}");
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        }

        Console.WriteLine($"IsAuthenticated: {_currentUser.Identity?.IsAuthenticated}");
        return new AuthenticationState(_currentUser);
    }

    public async Task MarkUserAsAuthenticated(string username)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, username)
        }, "Custom");
        
        _currentUser = new ClaimsPrincipal(identity);

        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "username", username);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task MarkUserAsLoggedOut()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "username");
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}