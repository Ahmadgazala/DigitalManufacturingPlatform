using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Services;

public interface IFileService
{
    Task<string?> SaveImageAsync(IFormFile? file, string folder);
    Task<string?> SaveFileAsync(IFormFile? file, string folder);
    Task DeleteAsync(string? url);
}

public class FileService : IFileService
{
    private readonly ApplicationDbContext _db;
    private readonly long _maxImageBytes = 5 * 1024 * 1024; // 5 MB
    private readonly long _maxFileBytes  = 20 * 1024 * 1024; // 20 MB

    private static readonly string[] AllowedImages = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] AllowedFiles  = { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf", ".dxf", ".svg", ".stl", ".step", ".stp", ".zip" };

    public FileService(ApplicationDbContext db) => _db = db;

    public async Task<string?> SaveImageAsync(IFormFile? file, string folder)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > _maxImageBytes) throw new InvalidOperationException("حجم الصورة يتجاوز 5 ميجابايت");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImages.Contains(ext)) throw new InvalidOperationException("صيغة الصورة غير مدعومة");
        return await SaveAsync(file, folder);
    }

    public async Task<string?> SaveFileAsync(IFormFile? file, string folder)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > _maxFileBytes) throw new InvalidOperationException("حجم الملف يتجاوز 20 ميجابايت");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedFiles.Contains(ext)) throw new InvalidOperationException("صيغة الملف غير مدعومة");
        return await SaveAsync(file, folder);
    }

    private async Task<string> SaveAsync(IFormFile file, string folder)
    {
        var entry = new StoredFile
        {
            Folder      = folder,
            FileName    = Path.GetFileName(file.FileName),
            ContentType = file.ContentType
        };
        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        entry.Data = ms.ToArray();

        _db.StoredFiles.Add(entry);
        await _db.SaveChangesAsync();
        return $"/files/{entry.Id}";
    }

    public async Task DeleteAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("/files/")) return;
        var id = url.Substring("/files/".Length);
        var file = await _db.StoredFiles.FindAsync(id);
        if (file != null)
        {
            _db.StoredFiles.Remove(file);
            await _db.SaveChangesAsync();
        }
    }
}