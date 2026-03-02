using NewsFlow.Application.Common.Interfaces;

namespace NewsFlow.Web.Services;

public class BlazorCurrentUserService : ICurrentUserService
{
    private readonly AuthStateService _auth;

    public BlazorCurrentUserService(AuthStateService auth) => _auth = auth;

    public Guid? UserId => _auth.CurrentUser?.Id;
    public string? UserName => _auth.CurrentUser?.Username;
    public IReadOnlyList<string> Roles => _auth.CurrentUser?.Roles ?? [];
    public bool IsAuthenticated => _auth.IsAuthenticated;
}
