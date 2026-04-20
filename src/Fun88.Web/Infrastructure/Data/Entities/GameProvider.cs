namespace Fun88.Web.Infrastructure.Data.Entities;

public class GameProvider
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Game> Games { get; set; } = [];
}
