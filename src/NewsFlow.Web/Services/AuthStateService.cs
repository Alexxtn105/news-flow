using NewsFlow.Application.Auth.DTOs;

namespace NewsFlow.Web.Services;

public class AuthStateService
{
    public UserInfo? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public event Action? OnChange;

    public void Login(UserInfo user)
    {
        CurrentUser = user;
        OnChange?.Invoke();
    }

    public void Logout()
    {
        CurrentUser = null;
        OnChange?.Invoke();
    }

    public bool HasRole(string role) => CurrentUser?.Roles.Contains(role) == true;
}
