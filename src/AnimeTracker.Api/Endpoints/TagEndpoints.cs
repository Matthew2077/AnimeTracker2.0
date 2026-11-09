using AnimeTracker.Api.Data;
using AnimeTracker.Api.Dtos;
using AnimeTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AnimeTracker.Api.Endpoints;

public static class TagEndpoints
{
    public static void MapTagEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/tags");

        g.MapGet("/", async (AppDbContext db) =>
        {
            var tags = await db.Tags.ToListAsync();
            return tags.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                .Select(t => new TagDto(t.Id, t.Name, t.Color)).ToList();
        });

        g.MapPost("/", async (TagInput input, AppDbContext db) =>
        {
            var name = input.Name?.Trim() ?? "";
            if (name.Length is 0 or > 50)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Name is required (max 50 characters)."] });
            if (await db.Tags.AnyAsync(t => t.Name == name))
                return Results.Conflict(new { error = "A tag with this name already exists." });

            var tag = new Tag { Name = name, Color = input.Color };
            db.Tags.Add(tag);
            await db.SaveChangesAsync();
            return Results.Created($"/api/tags/{tag.Id}", new TagDto(tag.Id, tag.Name, tag.Color));
        });

        g.MapPut("/{id:int}", async (int id, TagInput input, AppDbContext db) =>
        {
            var name = input.Name?.Trim() ?? "";
            if (name.Length is 0 or > 50)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Name is required (max 50 characters)."] });

            var tag = await db.Tags.FindAsync(id);
            if (tag is null) return Results.NotFound();
            if (await db.Tags.AnyAsync(t => t.Name == name && t.Id != id))
                return Results.Conflict(new { error = "A tag with this name already exists." });

            tag.Name = name;
            tag.Color = input.Color;
            await db.SaveChangesAsync();
            return Results.Ok(new TagDto(tag.Id, tag.Name, tag.Color));
        });

        // Toglie il tag dalle entry (cascade sulla tabella di join)
        g.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var n = await db.Tags.Where(t => t.Id == id).ExecuteDeleteAsync();
            return n == 0 ? Results.NotFound() : Results.NoContent();
        });
    }
}