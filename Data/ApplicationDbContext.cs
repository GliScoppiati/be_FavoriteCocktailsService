using Microsoft.EntityFrameworkCore;
using FavoriteCocktailsService.Models;

namespace FavoriteCocktailsService.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<FavoriteCocktail> FavoriteCocktails { get; set; }
}