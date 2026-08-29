using Microsoft.AspNetCore.Mvc;
using DMP.Web.Data;

namespace DMP.Web.Controllers;

public class FilesController : Controller
{
    private readonly ApplicationDbContext _db;

    public FilesController(ApplicationDbContext db) => _db = db;

    [HttpGet("files/{id?}")]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();
        var file = await _db.StoredFiles.FindAsync(id);
        if (file == null) return NotFound();
        return File(file.Data, file.ContentType ?? "application/octet-stream");
    }
}