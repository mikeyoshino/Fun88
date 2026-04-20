namespace Fun88.Web.Modules.Admin.Services;

using Microsoft.Extensions.Logging;
using Supabase;

public class AdminAuthService(Client supabaseClient, ILogger<AdminAuthService> logger) : IAdminAuthService
{
    public async Task<string?> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        try
        {
            var session = await supabaseClient.Auth.SignIn(email, password);
            return session?.AccessToken;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Admin sign-in failed for {Email}", email);
            return null;
        }
    }

    public async Task SignOutAsync(CancellationToken ct = default)
    {
        try { await supabaseClient.Auth.SignOut(); }
        catch (Exception ex) { logger.LogWarning(ex, "Sign-out error"); }
    }
}
