namespace FavoriteCocktailsService.DTOs;

public class FavoriteCocktailDto
{
    public string CocktailId { get; set; } = string.Empty;
    public DateTime FavoritedAt { get; set; }
}