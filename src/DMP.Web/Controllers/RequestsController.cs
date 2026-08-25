using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;

    public RequestsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileService fileService,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T)
    {
        _db = db;
        _userManager = userManager;
        _fileService = fileService;
        _T = T;
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

        // CustomerId يُضبط من الجلسة وليس من الفورم — استثنه من التحقق
        ModelState.Remove(nameof(ManufacturingRequest.CustomerId));

        if (!ModelState.IsValid)
            return View(model);

        var userId = _userManager.GetUserId(User)!;
        model.CustomerId = userId;
        model.Status = RequestStatus.Open;
        model.IsApproved = false;

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

        TempData["Success"] = _T["تم إرسال طلبك بنجاح. بانتظار موافقة الإدارة قبل ظهوره للمصنّعين."].Value;
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    // GET: /Requests/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId  = _userManager.GetUserId(User)!;
        var isAdmin = User.IsInRole(SeedData.AdminRole);

        var request = await _db.ManufacturingRequests
            .Include(r => r.Files)
            .Include(r => r.Quotations)
                .ThenInclude(q => q.Manufacturer)
            .Include(r => r.OrderUpdates)
            .FirstOrDefaultAsync(r => r.Id == id && (isAdmin || r.CustomerId == userId));

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

        request.Status        = RequestStatus.Completed;
        request.TrackingStage = TrackingStage.Delivered;

        _db.OrderUpdates.Add(new OrderUpdate
        {
            RequestId = request.Id,
            Stage     = TrackingStage.Delivered,
            Note      = _T["تم تأكيد الاستلام من قِبل العميل"].Value
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم تحديد الطلب كمكتمل."].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Requests/UpdateTracking  (للمصنّع فقط)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manufacturer")]
    public async Task<IActionResult> UpdateTracking(int requestId, TrackingStage stage, string? note)
    {
        var userId = _userManager.GetUserId(User)!;
        var mfr    = await _db.Manufacturers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (mfr == null) return Forbid();

        var request = await _db.ManufacturingRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.ManufacturerId == mfr.Id);
        if (request == null) return NotFound();

        request.TrackingStage = stage;

        _db.OrderUpdates.Add(new OrderUpdate
        {
            RequestId = requestId,
            Stage     = stage,
            Note      = note
        });

        // إشعار للعميل
        _db.Notifications.Add(new Notification
        {
            UserId  = request.CustomerId,
            Message = _T["تحديث على طلبك \"{0}\": {1}", request.Title, stage.ToArabic()].Value,
            Link    = Url.Action("Details", "Requests", new { id = requestId }),
            IsRead  = false
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم تحديث حالة الطلب إلى: {0}", stage.ToArabic()].Value;
        return RedirectToAction("Dashboard", "Manufacturers");
    }
}
