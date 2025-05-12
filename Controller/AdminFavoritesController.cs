using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FavoriteCocktailsService.Data;
using FavoriteCocktailsService.DTOs;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore;

namespace FavoriteCocktailsService.Controllers;

[Authorize]
[ApiController]
[Route("api/favorites/global")]
public class AdminFavoritesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public AdminFavoritesController(ApplicationDbContext db) => _db = db;

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularCocktails([FromQuery] int top = 10)
    {
        var allowed = new[] { 1, 3, 5, 10, 20, 50 };

        if (!allowed.Contains(top))
        {
            return BadRequest(new
            {
                error = $"Valore 'top' non valido. Ammessi solo: {string.Join(", ", allowed)}"
            });
        }

        var stats = await _db.FavoriteCocktails
            .GroupBy(f => f.CocktailId)
            .Select(g => new
            {
                CocktailId = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(g => g.Count)
            .Take(top)
            .ToListAsync();

        return Ok(stats);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("trend/{cocktailId}")]
    public async Task<IActionResult> GetCocktailTrend(string cocktailId, [FromQuery] string interval = "day")
    {
        var now = DateTime.UtcNow;
        var fromDate = interval switch
        {
            "day" => now.AddDays(-30),
            "month" => now.AddMonths(-12),
            _ => now.AddDays(-30)
        };

        // ✅ Filtro lato DB
        var rawData = await _db.FavoriteCocktails
            .Where(f => f.CocktailId == cocktailId && f.FavoritedAt >= fromDate)
            .ToListAsync(); // ← scarico in memoria

        // ✅ Raggruppo in memoria
        var grouped = rawData
            .GroupBy(f => interval switch
            {
                "day" => f.FavoritedAt.Date,
                "month" => new DateTime(f.FavoritedAt.Year, f.FavoritedAt.Month, 1),
                _ => f.FavoritedAt.Date
            })
            .Select(g => new
            {
                Period = g.Key,
                Count = g.Count()
            })
            .OrderBy(g => g.Period);

        return Ok(grouped);
    }
}
