namespace Fun88.Web.Modules.Users.Services;

using Fun88.Web.Infrastructure.Data.Entities;

public interface IUserSyncService
{
    Task<User> SyncAsync(Supabase.Gotrue.User authUser, CancellationToken ct = default);
}
