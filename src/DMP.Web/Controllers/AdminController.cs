using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

[Authorize(Roles = SeedData.AdminRole)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T)
    {
        _db = db;
        _userManager = userManager;
        _T = T;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalManufacturers = await _db.Manufacturers.CountAsync();
        ViewBag.PendingManufacturers = await _db.Manufacturers.CountAsync(m => !m.IsApproved);
        ViewBag.TotalRequests = await _db.ManufacturingRequests.CountAsync();
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();
        ViewBag.TotalSuppliers = await _db.Suppliers.CountAsync();
        ViewBag.TotalCampaigns = await _db.GroupBuyingCampaigns.CountAsync(c => c.Status == CampaignStatus.Active);

        return View();
    }

    // GET: /Admin/Manufacturers
    public async Task<IActionResult> Manufacturers(string? searchTerm, string? filter)
    {
        var query = _db.Manufacturers
            .Include(m => m.User)
            .Include(m => m.Machines)
            .Include(m => m.Reviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(m =>
                m.WorkshopName.Contains(searchTerm) ||
                (m.City != null && m.City.Contains(searchTerm)) ||
                (m.User != null && m.User.FullName.Contains(searchTerm)));

        if (filter == "pending")
            query = query.Where(m => !m.IsApproved);
        else if (filter == "approved")
            query = query.Where(m => m.IsApproved);

        ViewBag.SearchTerm = searchTerm;
        ViewBag.Filter     = filter;

        var manufacturers = await query
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return View(manufacturers);
    }

    // POST: /Admin/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var manufacturer = await _db.Manufacturers.FindAsync(id);
        if (manufacturer == null)
            return NotFound();

        manufacturer.IsApproved = true;
        await _db.SaveChangesAsync();

        // إشعار للمصنّع
        _db.Notifications.Add(new Notification
        {
            UserId = manufacturer.UserId,
            Message = _T["تهانينا! تم اعتماد ورشتك على منصة Jo Maker."].Value,
            Link = "/Manufacturers/Dashboard",
            IsRead = false
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم اعتماد الورشة بنجاح."].Value;
        return RedirectToAction(nameof(Manufacturers));
    }

    // POST: /Admin/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var manufacturer = await _db.Manufacturers.FindAsync(id);
        if (manufacturer == null)
            return NotFound();

        manufacturer.IsApproved = false;
        await _db.SaveChangesAsync();

        // إشعار للمصنّع
        _db.Notifications.Add(new Notification
        {
            UserId = manufacturer.UserId,
            Message = _T["نأسف، لم يتم اعتماد ورشتك. يرجى مراجعة البيانات والتواصل مع الدعم."].Value,
            Link = "/Manufacturers/Edit",
            IsRead = false
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم رفض الورشة."].Value;
        return RedirectToAction(nameof(Manufacturers));
    }

    // GET: /Admin/Payments — مراجعة إيصالات الدفع
    public async Task<IActionResult> Payments()
    {
        var pending = await _db.ManufacturingRequests
            .Include(r => r.Customer)
            .Include(r => r.Quotations).ThenInclude(q => q.Manufacturer)
            .Where(r => r.PaymentStatus == PaymentStatus.UnderReview
                     || r.PaymentStatus == PaymentStatus.Paid
                     || r.PaymentStatus == PaymentStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(pending);
    }

    // POST: /Admin/ApprovePayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePayment(int requestId, string? note)
    {
        var request = await _db.ManufacturingRequests
            .Include(r => r.Quotations)
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null) return NotFound();

        var accepted = request.Quotations.FirstOrDefault(q => q.Status == QuotationStatus.Accepted);

        request.PaymentStatus     = PaymentStatus.Paid;
        request.PaidAt            = DateTime.UtcNow;
        request.PaidAmount        = accepted?.Price;
        request.PaymentReviewNote = note;

        // إشعار للعميل
        _db.Notifications.Add(new Notification
        {
            UserId    = request.CustomerId,
            Message   = _T["تم تأكيد دفعك لطلب رقم #{0}. سيبدأ المصنّع بالعمل قريباً.", requestId].Value,
            Link      = $"/Requests/Details/{requestId}",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم قبول الدفع للطلب #{0}.", requestId].Value;
        return RedirectToAction(nameof(Payments));
    }

    // POST: /Admin/RejectPayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPayment(int requestId, string? note)
    {
        var request = await _db.ManufacturingRequests
            .FirstOrDefaultAsync(r => r.Id == requestId);

        if (request == null) return NotFound();

        request.PaymentStatus     = PaymentStatus.NotPaid;
        request.PaymentReviewNote = note;

        // إشعار للعميل
        _db.Notifications.Add(new Notification
        {
            UserId    = request.CustomerId,
            Message   = _T["تم رفض إيصال الدفع للطلب #{0}. يرجى إعادة الدفع ورفع الإيصال الصحيح.", requestId].Value
                        + (string.IsNullOrEmpty(note) ? "" : _T[" السبب: {0}", note].Value),
            Link      = $"/Payments/Checkout?requestId={requestId}",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Error"] = _T["تم رفض الدفع للطلب #{0}.", requestId].Value;
        return RedirectToAction(nameof(Payments));
    }

    // GET: /Admin/Requests — كل طلبات التصنيع
    public async Task<IActionResult> Requests(string? status, string? search)
    {
        var query = _db.ManufacturingRequests
            .Include(r => r.Customer)
            .Include(r => r.Manufacturer)
            .Include(r => r.Quotations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r =>
                r.Title.Contains(search) ||
                (r.Customer != null && r.Customer.FullName.Contains(search)));

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RequestStatus>(status, out var s))
            query = query.Where(r => r.Status == s);

        ViewBag.StatusFilter = status;
        ViewBag.Search       = search;

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return View(requests);
    }

    // GET: /Admin/GroupBuying  →  يُعيد توجيه لصفحة الشراء الجماعي
    public IActionResult GroupBuying()
        => RedirectToAction("Index", "GroupBuying");

    // GET: /Admin/Users
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        // إضافة أدوار المستخدمين
        var usersWithRoles = new List<(ApplicationUser User, IList<string> Roles)>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            usersWithRoles.Add((user, roles));
        }

        ViewBag.UsersWithRoles = usersWithRoles;
        return View(users);
    }
}
