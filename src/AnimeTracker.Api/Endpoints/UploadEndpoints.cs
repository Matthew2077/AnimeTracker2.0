using AnimeTracker.Api.Services;

namespace AnimeTracker.Api.Endpoints;

public static class UploadEndpoints
{
    private const long MaxBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    private static bool LooksLikeImage(byte[] h, int len)
    {
        if (len < 12) return false;
        return (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF)                                   // jpeg
            || (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47)                   // png
            || (h[0] == (byte)'G' && h[1] == (byte)'I' && h[2] == (byte)'F' && h[3] == (byte)'8') // gif
            || (h[0] == (byte)'R' && h[1] == (byte)'I' && h[2] == (byte)'F' && h[3] == (byte)'F'
                && h[8] == (byte)'W' && h[9] == (byte)'E' && h[10] == (byte)'B' && h[11] == (byte)'P'); // webp
    }

    public static void MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/uploads", async (IFormFile file, AppPaths paths) =>
        {
            if (file.Length == 0)
                return Results.BadRequest(new { error = "Empty file." });
            if (file.Length > MaxBytes)
                return Results.BadRequest(new { error = "File too large (max 5 MB)." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return Results.BadRequest(new { error = "Allowed types: jpg, jpeg, png, webp, gif." });

            var header = new byte[12];
            int read;
            await using (var probe = file.OpenReadStream())
                read = await probe.ReadAsync(header.AsMemory(0, header.Length));
            if (!LooksLikeImage(header, read))
                return Results.BadRequest(new { error = "The file is not a valid image." });

            var name = $"{Guid.NewGuid():N}{ext}";
            await using (var src = file.OpenReadStream())
            await using (var dst = File.Create(Path.Combine(paths.UploadsDir, name)))
                await src.CopyToAsync(dst);

            return Results.Ok(new { fileName = name, url = $"/uploads/{name}" });
        }).DisableAntiforgery();
    }
}
