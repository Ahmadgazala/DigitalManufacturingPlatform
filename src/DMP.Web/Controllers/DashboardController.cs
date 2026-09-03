using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;

    public DashboardController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T)
    {
        _db = db;
        _userManager = userManager;
        _T = T;
    }

    // GET: /Dashboard — لوحة المستخدم
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var activeStatuses = new[]
        {
            OrderStatus.Pending,
            OrderStatus.UnderReview,
            OrderStatus.Processing
        };

        var reviewCount = await _db.ProductReviews
            .CountAsync(r => r.CustomerUserId == userId);

        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.FullName = user.FullName;
        ViewBag.TotalOrders = orders.Count;
        ViewBag.ActiveOrders = orders.Count(o => activeStatuses.Contains(o.Status));
        ViewBag.TotalSpent = orders
            .Where(o => o.Status == OrderStatus.Paid)
            .Sum(o => o.TotalAmount);
        ViewBag.ReviewCount = reviewCount;
        ViewBag.Orders = orders.Take(5).ToList();
        ViewBag.Notifications = notifications;

        return View();
    }
}
