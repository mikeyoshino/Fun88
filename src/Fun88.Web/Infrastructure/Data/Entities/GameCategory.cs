namespace Fun88.Web.Infrastructure.Data.Entities;

public class GameCategory
{
    public Guid GameId { get; set; }
    public int CategoryId { get; set; }

    public Game Game { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
