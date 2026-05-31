using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET: /Notifications
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    // POST: /Notifications/MarkRead/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification == null)
            return NotFound();

        notification.IsRead = true;
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(notification.Link))
            return Redirect(notification.Link);

        return RedirectToAction(nameof(Index));
    }

    // GET: /Notifications/GetUnread
    [HttpGet]
    public async Task<IActionResult> GetUnread()
    {
        var userId = _userManager.GetUserId(User)!;

        var count = await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        return Json(new { count });
    }

    // POST: /Notifications/MarkAllRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _userManager.GetUserId(User)!;

        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        if (unread.Count > 0)
            await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // دالة مساعدة لإضافة إشعار
    public async Task AddNotification(string userId, string message, string? link = null)
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
