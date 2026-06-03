using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;
using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.Services;

namespace DMP.Web.Controllers;

public class GroupBuyingController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileService _fileService;
    private readonly IConfiguration _config;

    public GroupBuyingController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileService fileService,
        IConfiguration config)
    {
        _db = db;
        _userManager = userManager;
        _fileService = fileService;
        _config = config;
        Stripe.StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    // GET: /GroupBuying  —  متاح لجميع المستخدمين المسجّلين (قراءة فقط للعملاء)
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var campaigns = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(campaigns);
    }

    // GET: /GroupBuying/Details/5  —  متاح لجميع المستخدمين المسجّلين
    [Authorize]
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

        // التحقق من الحد الأدنى لكل مصنّع لاحقاً بعد تحميل الحملة

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

        // التحقق من الحد الأدنى لكل مصنّع
        if (campaign.MinOrderPerManufacturer > 0 && quantity < campaign.MinOrderPerManufacturer)
        {
            TempData["Error"] = $"الحد الأدنى للطلب في هذه الحملة هو {campaign.MinOrderPerManufacturer} وحدة لكل مصنّع.";
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
    public async Task<IActionResult> Create(GroupBuyingCampaign model, IFormFile? image,
        [FromForm] string? deadlineDate,
        [FromForm] string? individualPrice,
        [FromForm] string? groupPrice)
    {
        // تحليل التاريخ والأسعار بـ InvariantCulture لتفادي مشكلة الـ Arabic culture
        if (!string.IsNullOrEmpty(deadlineDate) &&
            DateTime.TryParseExact(deadlineDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedDate))
        {
            model.DeadlineDate = parsedDate;
            ModelState.Remove(nameof(model.DeadlineDate));
        }

        if (!string.IsNullOrEmpty(individualPrice) &&
            decimal.TryParse(individualPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var ip))
        {
            model.IndividualPrice = ip;
            ModelState.Remove(nameof(model.IndividualPrice));
        }

        if (!string.IsNullOrEmpty(groupPrice) &&
            decimal.TryParse(groupPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var gp))
        {
            model.GroupPrice = gp;
            ModelState.Remove(nameof(model.GroupPrice));
        }

        if (int.TryParse(Request.Form["MinOrderPerManufacturer"], out var mopCreate) && mopCreate >= 0)
        {
            model.MinOrderPerManufacturer = mopCreate;
            ModelState.Remove(nameof(model.MinOrderPerManufacturer));
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return View(model);
        }

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

    // GET: /GroupBuying/Edit/5
    [HttpGet]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> Edit(int id)
    {
        var campaign = await _db.GroupBuyingCampaigns.FindAsync(id);
        if (campaign == null)
            return NotFound();

        return View(campaign);
    }

    // POST: /GroupBuying/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> Edit(int id,
        [FromForm] string? deadlineDate,
        [FromForm] string? individualPrice,
        [FromForm] string? groupPrice,
        IFormFile? image)
    {
        var campaign = await _db.GroupBuyingCampaigns.FindAsync(id);
        if (campaign == null)
            return NotFound();

        // تحديث الحقول النصية
        campaign.Title       = Request.Form["Title"].ToString().Trim();
        campaign.Description = Request.Form["Description"].ToString().Trim();
        campaign.ItemName    = Request.Form["ItemName"].ToString().Trim();

        if (int.TryParse(Request.Form["MinQuantity"], out var mq) && mq >= 1)
            campaign.MinQuantity = mq;

        if (int.TryParse(Request.Form["MinOrderPerManufacturer"], out var mop) && mop >= 0)
            campaign.MinOrderPerManufacturer = mop;

        if (!string.IsNullOrEmpty(individualPrice) &&
            decimal.TryParse(individualPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var ip))
            campaign.IndividualPrice = ip;

        if (!string.IsNullOrEmpty(groupPrice) &&
            decimal.TryParse(groupPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var gp))
            campaign.GroupPrice = gp;

        if (!string.IsNullOrEmpty(deadlineDate) &&
            DateTime.TryParseExact(deadlineDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedDate))
            campaign.DeadlineDate = parsedDate;

        if (string.IsNullOrWhiteSpace(campaign.Title))
        {
            TempData["Error"] = "عنوان الحملة مطلوب.";
            return View(campaign);
        }

        try
        {
            if (image != null && image.Length > 0)
            {
                _fileService.Delete(campaign.ImagePath);
                campaign.ImagePath = await _fileService.SaveImageAsync(image, "campaigns");
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "تم تحديث الحملة بنجاح.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(campaign);
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

    // POST: /GroupBuying/RequestPayment/5  (Admin only)
    // يغيّر الحملة إلى "بانتظار الدفع" ويُرسل إشعاراً لكل المشاركين
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> RequestPayment(int id)
    {
        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (campaign == null) return NotFound();

        if (campaign.CurrentQuantity < campaign.MinQuantity)
        {
            TempData["Error"] = $"لم يكتمل النصاب بعد ({campaign.CurrentQuantity}/{campaign.MinQuantity}).";
            return RedirectToAction(nameof(Details), new { id });
        }

        // تحديث الحالة
        campaign.Status = CampaignStatus.AwaitingPayment;

        // تحديث حالة دفع كل مشارك → Pending
        foreach (var p in campaign.Participants)
            if (p.PaymentStatus == ParticipantPaymentStatus.NotRequested)
                p.PaymentStatus = ParticipantPaymentStatus.Pending;

        // إرسال إشعار لكل مشارك
        foreach (var p in campaign.Participants.Where(x => x.User != null))
        {
            _db.Notifications.Add(new Notification
            {
                UserId  = p.UserId,
                Message = $"💳 اكتمل النصاب — يرجى إتمام الدفع لحملة «{campaign.Title}». المبلغ المطلوب: {(campaign.GroupPrice * p.Quantity):N2} د.أ.",
                Link    = $"/GroupBuying/Details/{campaign.Id}"
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = $"تم إرسال طلب الدفع لـ {campaign.Participants.Count} مشارك.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /GroupBuying/PayNow/5  (participantId)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> PayNow(int campaignId)
    {
        var userId = _userManager.GetUserId(User)!;

        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null) return NotFound();

        var participant = campaign.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            TempData["Error"] = "أنت لست مشاركاً في هذه الحملة.";
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (participant.PaymentStatus == ParticipantPaymentStatus.Paid)
        {
            TempData["Error"] = "لقد دفعت مسبقاً لهذه الحملة.";
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        var totalAmount = campaign.GroupPrice * participant.Quantity;
        var domain = $"{Request.Scheme}://{Request.Host}";

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount  = (long)(campaign.GroupPrice * 100),
                        Currency    = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name        = $"حملة شراء جماعي: {campaign.Title}",
                            Description = $"المنتج: {campaign.ItemName} — الكمية: {participant.Quantity} وحدة"
                        }
                    },
                    Quantity = participant.Quantity
                }
            },
            Mode       = "payment",
            SuccessUrl = $"{domain}/GroupBuying/PaymentSuccess?campaignId={campaignId}&session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl  = $"{domain}/GroupBuying/Details/{campaignId}",
            Metadata   = new Dictionary<string, string>
            {
                { "campaignId",     campaignId.ToString() },
                { "participantId",  participant.Id.ToString() },
                { "userId",         userId }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        participant.StripeSessionId = session.Id;
        await _db.SaveChangesAsync();

        return Redirect(session.Url);
    }

    // GET: /GroupBuying/PaymentSuccess
    [Authorize]
    public async Task<IActionResult> PaymentSuccess(int campaignId, string session_id)
    {
        var userId = _userManager.GetUserId(User)!;

        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null) return NotFound();

        var participant = campaign.Participants
            .FirstOrDefault(p => p.UserId == userId && p.StripeSessionId == session_id);

        if (participant == null) return NotFound();

        // تحقق من Stripe
        var service = new SessionService();
        var session = await service.GetAsync(session_id);

        if (session.PaymentStatus == "paid")
        {
            participant.PaymentStatus = ParticipantPaymentStatus.Paid;
            participant.PaidAmount    = (decimal)(session.AmountTotal ?? 0) / 100;
            participant.PaidAt        = DateTime.UtcNow;

            // هل دفع الجميع؟ → تأكيد الحملة تلقائياً
            var allPaid = campaign.Participants.All(p => p.PaymentStatus == ParticipantPaymentStatus.Paid);
            if (allPaid)
            {
                campaign.Status = CampaignStatus.Confirmed;

                // إشعار للأدمن
                var admins = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
                foreach (var admin in admins)
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId  = admin.Id,
                        Message = $"✅ حملة مؤكدة — اكتملت جميع المدفوعات لحملة «{campaign.Title}». يمكنك إصدار أمر الشراء الآن.",
                        Link    = $"/GroupBuying/Details/{campaignId}"
                    });
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = allPaid
                ? "✅ تم دفعك بنجاح! اكتملت جميع المدفوعات وتم تأكيد الحملة."
                : "✅ تم دفعك بنجاح! بانتظار باقي المشاركين.";
        }
        else
        {
            TempData["Error"] = "لم يكتمل الدفع. يرجى المحاولة مجدداً.";
        }

        return RedirectToAction(nameof(Details), new { id = campaignId });
    }
}
