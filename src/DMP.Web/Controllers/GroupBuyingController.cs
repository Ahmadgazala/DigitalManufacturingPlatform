using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.Services;

namespace DMP.Web.Controllers;

public class GroupBuyingController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileService _fileService;

    public GroupBuyingController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileService fileService)
    {
        _db = db;
        _userManager = userManager;
        _fileService = fileService;
    }

    // GET: /GroupBuying
    [Authorize(Roles = $"{SeedData.ManufacturerRole},{SeedData.AdminRole}")]
    public async Task<IActionResult> Index()
    {
        var campaigns = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(campaigns);
    }

    // GET: /GroupBuying/Details/5
    [Authorize(Roles = $"{SeedData.ManufacturerRole},{SeedData.AdminRole}")]
    public async Task<IActionResult> Details(int id)
    {
        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (campaign == null)
            return NotFound();

        var userId = _userManager.GetUserId(User)!;
        var participation = campaign.Participants.FirstOrDefault(p => p.UserId == userId);

        bool alreadyJoined = participation != null;
        bool canWithdraw = false;
        DateTime? withdrawDeadline = null;

        if (participation != null)
        {
            withdrawDeadline = participation.JoinedAt.AddHours(48);
            canWithdraw = DateTime.UtcNow < withdrawDeadline
                          && campaign.Status == CampaignStatus.Active;
        }

        ViewBag.AlreadyJoined = alreadyJoined;
        ViewBag.CanWithdraw = canWithdraw;
        ViewBag.WithdrawDeadline = withdrawDeadline;

        return View(campaign);
    }

    // POST: /GroupBuying/Join
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> Join(int campaignId, int quantity, string? preferences, bool consent)
    {
        if (!consent)
        {
            TempData["Error"] = "يجب الموافقة على الشروط والأحكام للانضمام إلى الحملة.";
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (quantity < 1)
        {
            TempData["Error"] = "الكمية يجب أن تكون 1 على الأقل.";
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        var userId = _userManager.GetUserId(User)!;

        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return NotFound();

        if (!campaign.IsActive)
        {
            TempData["Error"] = "الحملة غير متاحة للانضمام.";
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        var alreadyJoined = campaign.Participants.Any(p => p.UserId == userId);
        if (alreadyJoined)
        {
            TempData["Error"] = "أنت منضم إلى هذه الحملة بالفعل.";
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        campaign.Participants.Add(new CampaignParticipant
        {
            CampaignId = campaignId,
            UserId = userId,
            Quantity = quantity,
            Preferences = preferences
        });

        campaign.CurrentQuantity += quantity;
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم انضمامك إلى الحملة بنجاح!";
        return RedirectToAction(nameof(Details), new { id = campaignId });
    }

    // POST: /GroupBuying/Leave
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> Leave(int campaignId)
    {
        var userId = _userManager.GetUserId(User)!;

        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return NotFound();

        var participation = campaign.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participation == null)
        {
            TempData["Error"] = "أنت لست منضماً إلى هذه الحملة.";
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        // السماح بالانسحاب فقط خلال 48 ساعة وقبل تأكيد الحملة
        var withdrawDeadline = participation.JoinedAt.AddHours(48);
        if (DateTime.UtcNow > withdrawDeadline)
        {
            TempData["Error"] = "انتهت مهلة الانسحاب (48 ساعة من وقت الانضمام).";
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (campaign.Status != CampaignStatus.Active)
        {
            TempData["Error"] = "لا يمكن الانسحاب بعد تأكيد الحملة.";
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        campaign.CurrentQuantity -= participation.Quantity;
        if (campaign.CurrentQuantity < 0)
            campaign.CurrentQuantity = 0;

        _db.CampaignParticipants.Remove(participation);
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم انسحابك من الحملة.";
        return RedirectToAction(nameof(Index));
    }

    // GET: /GroupBuying/Create
    [HttpGet]
    [Authorize(Roles = SeedData.AdminRole)]
    public IActionResult Create()
    {
        return View(new GroupBuyingCampaign { DeadlineDate = DateTime.UtcNow.AddDays(30) });
    }

    // POST: /GroupBuying/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> Create(GroupBuyingCampaign model, IFormFile? image)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            if (image != null)
                model.ImagePath = await _fileService.SaveImageAsync(image, "campaigns");

            model.Status = CampaignStatus.Active;
            model.CurrentQuantity = 0;

            _db.GroupBuyingCampaigns.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "تم إنشاء الحملة بنجاح.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(model);
        }
    }

    // POST: /GroupBuying/UpdateStatus
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> UpdateStatus(int id, CampaignStatus status)
    {
        var campaign = await _db.GroupBuyingCampaigns.FindAsync(id);
        if (campaign == null)
            return NotFound();

        campaign.Status = status;
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم تحديث حالة الحملة.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
