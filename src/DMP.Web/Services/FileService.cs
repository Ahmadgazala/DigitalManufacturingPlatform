namespace DMP.Web.Services;

public interface IFileService
{
    Task<string?> SaveImageAsync(IFormFile? file, string folder);
    Task<string?> SaveFileAsync(IFormFile? file, string folder);
    void Delete(string? relativePath);
}

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly long _maxImageBytes = 5 * 1024 * 1024; // 5 MB
    private readonly long _maxFileBytes  = 20 * 1024 * 1024; // 20 MB

    private static readonly string[] AllowedImages = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private static readonly string[] AllowedFiles  = { ".jpg", ".jpeg", ".png", ".pdf", ".dxf", ".svg", ".stl", ".step", ".stp", ".zip" };

    public FileService(IWebHostEnvironment env) => _env = env;

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
        var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var fullPath = Path.Combine(dir, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);
        return $"/uploads/{folder}/{fileName}";
    }

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;
        var full = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/'));
        if (File.Exists(full)) File.Delete(full);
    }
}
