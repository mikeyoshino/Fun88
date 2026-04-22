namespace Fun88.Web.Modules.Users.Services;

using Fun88.Web.Infrastructure.Data.Entities;
using Supabase;

public class UserSyncService(Client supabaseClient) : IUserSyncService
{
    public async Task<User> SyncAsync(Supabase.Gotrue.User authUser, CancellationToken ct = default)
    {
        var existing = await supabaseClient.From<User>()
            .Filter("id", Postgrest.Constants.Operator.Equals, authUser.Id)
            .Single(ct);

        if (existing is null)
        {
            var newUser = new User
            {
                Id = Guid.TryParse(authUser.Id, out var guid) ? guid : Guid.NewGuid(),
                Username = authUser.Email?.Split('@')[0] ?? authUser.Id ?? "user",
                PreferredLanguage = "en",
                CreatedAt = DateTime.UtcNow
            };

            var inserted = await supabaseClient.From<User>()
                .Insert(newUser, new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation }, ct);

            return inserted.Model ?? newUser;
        }
        else
        {
            existing.LastLoginAt = DateTime.UtcNow;
            await supabaseClient.From<User>()
                .Match(new Dictionary<string, string> { ["id"] = authUser.Id! })
                .Update(existing, cancellationToken: ct);

            return existing;
        }
    }
}
