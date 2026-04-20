namespace Fun88.Web.Modules.Admin.Services;

public interface IAdminAuthService
{
    /// <summary>Sign in against Supabase Auth. Returns the session access token on success, null on failure.</summary>
    Task<string?> SignInAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Sign out from Supabase Auth.</summary>
    Task SignOutAsync(CancellationToken ct = default);
}
