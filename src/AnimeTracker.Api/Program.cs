using AnimeTracker.Api.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// DB in <root soluzione>/data/tracker.db
var dbPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "data", "tracker.db"));
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddOpenApi();

var app = builder.Build();

// Applica le migrazioni all'avvio
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();   // UI su /scalar
}

app.MapGet("/api/health", () => new { status = "ok" });

app.MapGet("/api/genres", async (AppDbContext db) =>
    await db.Genres.OrderBy(g => g.Name).Select(g => new { g.Id, g.Name }).ToListAsync());

app.Run();