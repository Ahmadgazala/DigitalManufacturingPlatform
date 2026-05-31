using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

[Authorize(Roles = SeedData.AdminRole)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
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
    public async Task<IActionResult> Manufacturers()
    {
        var manufacturers = await _db.Manufacturers
            .Include(m => m.User)
            .Include(m => m.Machines)
            .Include(m => m.Reviews)
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
            Message = "تهانينا! تم اعتماد ورشتك على منصة Jo Maker.",
            Link = "/Manufacturers/Dashboard",
            IsRead = false
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم اعتماد الورشة بنجاح.";
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
            Message = "نأسف، لم يتم اعتماد ورشتك. يرجى مراجعة البيانات والتواصل مع الدعم.",
            Link = "/Manufacturers/Edit",
            IsRead = false
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم رفض الورشة.";
        return RedirectToAction(nameof(Manufacturers));
    }

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
