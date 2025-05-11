namespace FavoriteCocktailsService.Models;

public class FavoriteCocktail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string CocktailId { get; set; } = string.Empty;
    public DateTime FavoritedAt { get; set; } = DateTime.UtcNow;
}