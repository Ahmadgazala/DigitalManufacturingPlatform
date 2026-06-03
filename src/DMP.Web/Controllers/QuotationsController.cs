using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

public class QuotationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public QuotationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private async Task<Manufacturer?> GetCurrentManufacturerAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        return await _db.Manufacturers
            .Include(m => m.Machines)
            .FirstOrDefaultAsync(m => m.UserId == userId);
    }

    // GET: /Quotations/IncomingRequests
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> IncomingRequests()
    {
        var manufacturer = await GetCurrentManufacturerAsync();
        if (manufacturer == null)
        {
            TempData["Error"] = "يرجى إنشاء ملف الورشة أولاً.";
            return RedirectToAction("Edit", "Manufacturers");
        }

        var categories = manufacturer.Machines.Select(m => m.Category).Distinct().ToList();

        // الطلبات المفتوحة التي لم يتقدم عليها هذا المصنّع بعد
        var submittedRequestIds = await _db.Quotations
            .Where(q => q.ManufacturerId == manufacturer.Id)
            .Select(q => q.RequestId)
            .ToListAsync();

        var requests = await _db.ManufacturingRequests
            .Include(r => r.Customer)
            .Include(r => r.Files)
            .Where(r => r.Status == RequestStatus.Open
                        && categories.Contains(r.Category)
                        && !submittedRequestIds.Contains(r.Id))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ViewBag.Manufacturer = manufacturer;
        return View(requests);
    }

    // GET: /Quotations/Submit/5
    [HttpGet]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> Submit(int requestId)
    {
        var manufacturer = await GetCurrentManufacturerAsync();
        if (manufacturer == null)
            return Forbid();

        var request = await _db.ManufacturingRequests
            .Include(r => r.Customer)
            .Include(r => r.Files)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.Status == RequestStatus.Open);

        if (request == null)
            return NotFound();

        var alreadySubmitted = await _db.Quotations
            .AnyAsync(q => q.RequestId == requestId && q.ManufacturerId == manufacturer.Id);

        if (alreadySubmitted)
        {
            TempData["Error"] = "لقد أرسلت عرض سعر لهذا الطلب مسبقاً.";
            return RedirectToAction(nameof(IncomingRequests));
        }

        ViewBag.Request = request;
        ViewBag.ManufacturerId = manufacturer.Id;

        return View(new Quotation { RequestId = requestId, ManufacturerId = manufacturer.Id });
    }

    // POST: /Quotations/Submit
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> Submit(Quotation model)
    {
        var manufacturer = await GetCurrentManufacturerAsync();
        if (manufacturer == null)
            return Forbid();

        model.ManufacturerId = manufacturer.Id;
        model.Status = QuotationStatus.Pending;

        if (!ModelState.IsValid)
        {
            var req = await _db.ManufacturingRequests
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Id == model.RequestId);
            ViewBag.Request = req;
            ViewBag.ManufacturerId = manufacturer.Id;
            return View(model);
        }

        var alreadySubmitted = await _db.Quotations
            .AnyAsync(q => q.RequestId == model.RequestId && q.ManufacturerId == manufacturer.Id);

        if (alreadySubmitted)
        {
            TempData["Error"] = "لقد أرسلت عرض سعر لهذا الطلب مسبقاً.";
            return RedirectToAction(nameof(IncomingRequests));
        }

        _db.Quotations.Add(model);
        await _db.SaveChangesAsync();

        // إشعار لصاحب الطلب
        var request = await _db.ManufacturingRequests.FindAsync(model.RequestId);
        if (request != null)
        {
            await AddNotificationAsync(
                request.CustomerId,
                $"وصلك عرض سعر جديد على طلبك: {request.Title}",
                Url.Action("Details", "Requests", new { id = request.Id }));
        }

        TempData["Success"] = "تم إرسال عرض السعر بنجاح.";
        return RedirectToAction(nameof(IncomingRequests));
    }

    // POST: /Quotations/Accept/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Accept(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var quotation = await _db.Quotations
            .Include(q => q.Request)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null || quotation.Request == null)
            return NotFound();

        if (quotation.Request.CustomerId != userId)
            return Forbid();

        quotation.Status = QuotationStatus.Accepted;
        quotation.Request.Status        = RequestStatus.InProgress;
        quotation.Request.ManufacturerId = quotation.ManufacturerId;
        quotation.Request.TrackingStage  = TrackingStage.Received;

        // سجل تتبع أولي
        _db.OrderUpdates.Add(new OrderUpdate
        {
            RequestId = quotation.RequestId,
            Stage     = TrackingStage.Received,
            Note      = "تم قبول العرض وبدء التنفيذ"
        });

        // رفض باقي العروض تلقائياً
        var otherQuotations = await _db.Quotations
            .Where(q => q.RequestId == quotation.RequestId && q.Id != id && q.Status == QuotationStatus.Pending)
            .ToListAsync();

        foreach (var q in otherQuotations)
            q.Status = QuotationStatus.Rejected;

        await _db.SaveChangesAsync();

        // إشعار للمصنّع
        var manufacturer = await _db.Manufacturers.FindAsync(quotation.ManufacturerId);
        if (manufacturer != null)
        {
            await AddNotificationAsync(
                manufacturer.UserId,
                $"تم قبول عرض سعرك على طلب: {quotation.Request.Title}",
                Url.Action("Dashboard", "Manufacturers"));
        }

        TempData["Success"] = "تم قبول العرض وسيتواصل معك المصنّع قريباً.";
        return RedirectToAction("Details", "Requests", new { id = quotation.RequestId });
    }

    // POST: /Quotations/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Reject(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var quotation = await _db.Quotations
            .Include(q => q.Request)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quotation == null || quotation.Request == null)
            return NotFound();

        if (quotation.Request.CustomerId != userId)
            return Forbid();

        quotation.Status = QuotationStatus.Rejected;
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم رفض العرض.";
        return RedirectToAction("Details", "Requests", new { id = quotation.RequestId });
    }

    private async Task AddNotificationAsync(string userId, string message, string? link)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Message = message,
            Link = link,
            IsRead = false
        });
        await _db.SaveChangesAsync();
    }
}
