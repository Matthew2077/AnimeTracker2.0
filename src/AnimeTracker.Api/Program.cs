using System.Text.Json.Serialization;
using AnimeTracker.Api.Data;
using AnimeTracker.Api.Endpoints;
using AnimeTracker.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// data/ e uploads/ nella root della soluzione
var rootDir = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
var paths = new AppPaths(Path.Combine(rootDir, "data"), Path.Combine(rootDir, "uploads"));
Directory.CreateDirectory(paths.DataDir);
Directory.CreateDirectory(paths.UploadsDir);
builder.Services.AddSingleton(paths);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite($"Data Source={Path.Combine(paths.DataDir, "tracker.db")}"));

// Enum come stringhe nel JSON ("Ongoing", "LightNovel", ...)
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddScoped<OrderingService>();
builder.Services.AddScoped<StatsService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();   // UI su /scalar
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(paths.UploadsDir),
    RequestPath = "/uploads"
});

app.MapMiscEndpoints();
app.MapSeriesEndpoints();
app.MapEntryEndpoints();
app.MapTagEndpoints();
app.MapUploadEndpoints();

app.Run();
