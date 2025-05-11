using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FavoriteCocktailsService.Data;
using FavoriteCocktailsService.Models;
using System.Security.Claims;
using FavoriteCocktailsService.DTOs;

namespace FavoriteCocktailsService.Controllers;

[Authorize]
[ApiController]
[Route("api/favorites")]
public class UserFavoritesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public UserFavoritesController(ApplicationDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> AddFavorite([FromBody] FavoriteCocktailRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var userGuid = Guid.Parse(userId);

        var exists = await _db.FavoriteCocktails.AnyAsync(f => f.UserId == userGuid && f.CocktailId == request.CocktailId);
        if (exists) return Conflict("Already in favorites");

        _db.FavoriteCocktails.Add(new FavoriteCocktail
        {
            UserId = userGuid,
            CocktailId = request.CocktailId
        });
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var userGuid = Guid.Parse(userId);

        var favorites = await _db.FavoriteCocktails
            .Where(f => f.UserId == userGuid)
            .OrderByDescending(f => f.FavoritedAt)
            .Select(f => new FavoriteCocktailDto
            {
                CocktailId = f.CocktailId,
                FavoritedAt = f.FavoritedAt
            })
            .ToListAsync();

        return Ok(favorites);
    }

    [HttpDelete("{cocktailId}")]
    public async Task<IActionResult> RemoveFavorite(string cocktailId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var userGuid = Guid.Parse(userId);

        var favorite = await _db.FavoriteCocktails.FirstOrDefaultAsync(f => f.UserId == userGuid && f.CocktailId == cocktailId);
        if (favorite == null) return NotFound();

        _db.FavoriteCocktails.Remove(favorite);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("mine/{cocktailId}")]
    public async Task<IActionResult> IsFavorite(string cocktailId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var userGuid = Guid.Parse(userId);

        var exists = await _db.FavoriteCocktails
            .AnyAsync(f => f.UserId == userGuid && f.CocktailId == cocktailId);

        return Ok(new { isFavorite = exists });
    }

    [HttpGet("recommended")]
    public async Task<IActionResult> GetRecommendations([FromQuery] int limit = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var userGuid = Guid.Parse(userId);

        // 1. Cocktail già preferiti dall'utente
        var myCocktails = await _db.FavoriteCocktails
            .Where(f => f.UserId == userGuid)
            .Select(f => f.CocktailId)
            .ToListAsync();

        if (!myCocktails.Any())
            return Ok(new List<object>()); // nessuna base per i suggerimenti

        // 2. Utenti diversi che hanno preferito almeno 1 cocktail in comune
        var similarUsers = await _db.FavoriteCocktails
            .Where(f => myCocktails.Contains(f.CocktailId) && f.UserId != userGuid)
            .Select(f => f.UserId)
            .Distinct()
            .ToListAsync();

        if (!similarUsers.Any())
            return Ok(new List<object>());

        // 3. Cocktail favoriti da utenti simili, esclusi quelli già presenti tra i preferiti dell'utente
        var recommended = await _db.FavoriteCocktails
            .Where(f => similarUsers.Contains(f.UserId) && !myCocktails.Contains(f.CocktailId))
            .GroupBy(f => f.CocktailId)
            .Select(g => new
            {
                CocktailId = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(g => g.Count)
            .Take(limit)
            .ToListAsync();

        return Ok(recommended);
    }
}