using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _env;

    public PaymentsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _env = env;
    }

    // GET: /Payments/Checkout/5
    [HttpGet]
    public async Task<IActionResult> Checkout(int requestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ManufacturingRequests
            .Include(r => r.Quotations).ThenInclude(q => q.Manufacturer)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CustomerId == userId);

        if (request == null) return NotFound();

        if (request.PaymentStatus == PaymentStatus.Paid)
        {
            TempData["Error"] = "تم الدفع مسبقاً لهذا الطلب.";
            return RedirectToAction("Details", "Requests", new { id = requestId });
        }

        if (request.PaymentStatus == PaymentStatus.UnderReview)
        {
            TempData["Error"] = "إيصال الدفع قيد المراجعة من المدير.";
            return RedirectToAction("Details", "Requests", new { id = requestId });
        }

        var accepted = request.Quotations.FirstOrDefault(q => q.Status == QuotationStatus.Accepted);
        if (accepted == null)
        {
            TempData["Error"] = "لا يوجد عرض مقبول لهذا الطلب.";
            return RedirectToAction("Details", "Requests", new { id = requestId });
        }

        ViewBag.Request   = request;
        ViewBag.Quotation = accepted;
        return View();
    }

    // POST: /Payments/SubmitReceipt  — رفع صورة إيصال الدفع
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReceipt(int requestId, IFormFile receiptImage)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ManufacturingRequests
            .Include(r => r.Quotations).ThenInclude(q => q.Manufacturer)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CustomerId == userId);

        if (request == null) return NotFound();

        if (receiptImage == null || receiptImage.Length == 0)
        {
            TempData["Error"] = "يرجى رفع صورة إيصال الدفع.";
            return RedirectToAction("Checkout", new { requestId });
        }

        // التحقق من نوع الملف
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        var ext = Path.GetExtension(receiptImage.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
        {
            TempData["Error"] = "صيغة الملف غير مدعومة. يُسمح بـ JPG، PNG، PDF فقط.";
            return RedirectToAction("Checkout", new { requestId });
        }

        // حفظ الصورة
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "receipts");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"receipt_{requestId}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await receiptImage.CopyToAsync(stream);

        // تحديث الطلب
        request.PaymentReceiptPath = $"/uploads/receipts/{fileName}";
        request.PaymentStatus      = PaymentStatus.UnderReview;
        await _db.SaveChangesAsync();

        // إشعار للمدير
        var admin = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
        foreach (var adminUser in admin)
        {
            _db.Notifications.Add(new Notification
            {
                UserId    = adminUser.Id,
                Message   = $"إيصال دفع جديد بانتظار مراجعتك — طلب رقم #{requestId}",
                Link      = $"/Admin/Payments",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم رفع إيصال الدفع بنجاح. سيتم مراجعته من المدير وتأكيده خلال 24 ساعة.";
        return RedirectToAction("Details", "Requests", new { id = requestId });
    }

    // GET: /Payments/Cancel
    [HttpGet]
    public IActionResult Cancel(int requestId)
    {
        TempData["Error"] = "تم إلغاء عملية الدفع.";
        return RedirectToAction("Details", "Requests", new { id = requestId });
    }
}
