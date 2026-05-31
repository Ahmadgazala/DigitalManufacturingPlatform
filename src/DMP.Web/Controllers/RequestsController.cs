using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.Services;

namespace DMP.Web.Controllers;

[Authorize]
public class RequestsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileService _fileService;

    public RequestsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileService fileService)
    {
        _db = db;
        _userManager = userManager;
        _fileService = fileService;
    }

    // GET: /Requests
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var requests = await _db.ManufacturingRequests
            .Include(r => r.Quotations)
            .Where(r => r.CustomerId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    // GET: /Requests/Create
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Categories = Enum.GetValues<MachineCategory>();
        return View();
    }

    // POST: /Requests/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ManufacturingRequest model, List<IFormFile>? files)
    {
        ViewBag.Categories = Enum.GetValues<MachineCategory>();

        if (!ModelState.IsValid)
            return View(model);

        var userId = _userManager.GetUserId(User)!;
        model.CustomerId = userId;
        model.Status = RequestStatus.Open;

        _db.ManufacturingRequests.Add(model);
        await _db.SaveChangesAsync();

        // رفع الملفات المرفقة
        if (files != null && files.Count > 0)
        {
            foreach (var file in files)
            {
                try
                {
                    var path = await _fileService.SaveFileAsync(file, "requests");
                    if (path != null)
                    {
                        _db.RequestFiles.Add(new RequestFile
                        {
                            RequestId = model.Id,
                            FilePath = path,
                            OriginalFileName = file.FileName,
                            FileSizeBytes = file.Length
                        });
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                }
            }
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = "تم إرسال طلبك بنجاح. ستصلك عروض الأسعار قريباً.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    // GET: /Requests/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ManufacturingRequests
            .Include(r => r.Files)
            .Include(r => r.Quotations)
                .ThenInclude(q => q.Manufacturer)
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == userId);

        if (request == null)
            return NotFound();

        return View(request);
    }

    // POST: /Requests/MarkCompleted/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkCompleted(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ManufacturingRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == userId);

        if (request == null)
            return NotFound();

        request.Status = RequestStatus.Completed;
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم تحديد الطلب كمكتمل.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
