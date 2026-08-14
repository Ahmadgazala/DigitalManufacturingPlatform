using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public MessagesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET: /Messages/Inbox
    public async Task<IActionResult> Inbox()
    {
        var userId = _userManager.GetUserId(User)!;

        // جمع كل المحادثات (الرسائل المرسلة والمستقبلة)
        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        // تجميع المحادثات حسب الطرف الآخر
        var conversations = messages
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => new
            {
                OtherUserId = g.Key,
                OtherUser = g.First().SenderId == userId ? g.First().Receiver : g.First().Sender,
                LastMessage = g.First(),
                UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
            })
            .ToList();

        ViewBag.Conversations = conversations;
        return View();
    }

    // GET: /Messages/Conversation/userId  (also accepts ?otherUserId= or path segment id)
    public async Task<IActionResult> Conversation(string otherUserId, string? id = null)
    {
        otherUserId ??= id;
        var userId = _userManager.GetUserId(User)!;

        var otherUser = await _userManager.FindByIdAsync(otherUserId);
        if (otherUser == null)
            return NotFound();

        // تحديد الرسائل كمقروءة
        var unread = await _db.Messages
            .Where(m => m.SenderId == otherUserId && m.ReceiverId == userId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unread)
            msg.IsRead = true;

        if (unread.Count > 0)
            await _db.SaveChangesAsync();

        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId)
                     || (m.SenderId == otherUserId && m.ReceiverId == userId))
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        ViewBag.OtherUser = otherUser;
        ViewBag.OtherUserId = otherUserId;
        return View(messages);
    }

    // POST: /Messages/Send
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string receiverId, string body)
    {
        var userId = _userManager.GetUserId(User)!;

        if (string.IsNullOrWhiteSpace(body))
        {
            TempData["Error"] = "لا يمكن إرسال رسالة فارغة.";
            return RedirectToAction(nameof(Conversation), new { otherUserId = receiverId });
        }

        var receiver = await _userManager.FindByIdAsync(receiverId);
        if (receiver == null)
            return NotFound();

        _db.Messages.Add(new Message
        {
            SenderId = userId,
            ReceiverId = receiverId,
            Body = body.Trim(),
            IsRead = false
        });

        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Conversation), new { otherUserId = receiverId });
    }
}
