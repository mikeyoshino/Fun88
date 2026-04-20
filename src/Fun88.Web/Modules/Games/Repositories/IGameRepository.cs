namespace Fun88.Web.Modules.Games.Repositories;

using Fun88.Web.Infrastructure.Data.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IGameRepository
{
    Task<Game?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Game?> GetByProviderGameIdAsync(int providerId, string providerGameId, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetNewestAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetMostPopularAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetByCategorySlugAsync(string categorySlug, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> SearchAsync(string query, string languageCode, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetAllPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> CountByCategorySlugAsync(string categorySlug, CancellationToken ct = default);
    Task<int> CountAllAsync(CancellationToken ct = default);
    Task AddAsync(Game game, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}
