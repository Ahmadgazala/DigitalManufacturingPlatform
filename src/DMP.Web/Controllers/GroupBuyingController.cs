using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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
    private readonly IWebHostEnvironment _env;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;

    public GroupBuyingController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileService fileService,
        IWebHostEnvironment env,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T)
    {
        _db = db;
        _userManager = userManager;
        _fileService = fileService;
        _env = env;
        _T = T;
    }

    // GET: /GroupBuying
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var campaigns = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(campaigns);
    }

    // GET: /GroupBuying/Details/5
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
        bool canWithdraw   = false;
        DateTime? withdrawDeadline = null;

        if (participation != null)
        {
            withdrawDeadline = participation.JoinedAt.AddHours(48);
            canWithdraw = DateTime.UtcNow < withdrawDeadline
                          && campaign.CurrentQuantity < campaign.MinQuantity
                          && campaign.Status == CampaignStatus.Active;
        }

        ViewBag.AlreadyJoined    = alreadyJoined;
        ViewBag.CanWithdraw      = canWithdraw;
        ViewBag.WithdrawDeadline = withdrawDeadline;
        ViewBag.Participation    = participation;

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
            TempData["Error"] = _T["يجب الموافقة على الشروط والأحكام للانضمام إلى الحملة."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (quantity < 1)
        {
            TempData["Error"] = _T["الكمية يجب أن تكون 1 على الأقل."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        var userId = _userManager.GetUserId(User)!;
        if (await _userManager.FindByIdAsync(userId) == null)
            return RedirectToAction("Logout", "Account");

        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null)
            return NotFound();

        if (!campaign.IsActive)
        {
            TempData["Error"] = _T["الحملة غير متاحة للانضمام."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (campaign.Participants.Any(p => p.UserId == userId))
        {
            TempData["Error"] = _T["أنت منضم إلى هذه الحملة بالفعل."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (campaign.MinOrderPerManufacturer > 0 && quantity < campaign.MinOrderPerManufacturer)
        {
            TempData["Error"] = _T["الحد الأدنى للطلب في هذه الحملة هو {0} وحدة لكل مصنّع.", campaign.MinOrderPerManufacturer].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        campaign.Participants.Add(new CampaignParticipant
        {
            CampaignId  = campaignId,
            UserId      = userId,
            Quantity    = quantity,
            Preferences = preferences
        });

        campaign.CurrentQuantity += quantity;
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم انضمامك إلى الحملة! يرجى دفع العربون (نصف المبلغ) لتأكيد مشاركتك."].Value;
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
            TempData["Error"] = _T["أنت لست منضماً إلى هذه الحملة."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        var withdrawDeadline = participation.JoinedAt.AddHours(48);
        if (DateTime.UtcNow > withdrawDeadline)
        {
            TempData["Error"] = _T["انتهت مهلة الانسحاب (48 ساعة من وقت الانضمام)."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (campaign.CurrentQuantity >= campaign.MinQuantity)
        {
            TempData["Error"] = _T["لا يمكن الانسحاب بعد اكتمال النصاب."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (campaign.Status != CampaignStatus.Active)
        {
            TempData["Error"] = _T["لا يمكن الانسحاب بعد تأكيد الحملة."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        // إذا كان قد دفع عربوناً → نُشعر المدير باسترجاع المبلغ
        bool needsRefund = participation.PaymentStatus == ParticipantPaymentStatus.DepositPaid
                        || participation.PaymentStatus == ParticipantPaymentStatus.DepositUnderReview;

        if (needsRefund)
        {
            participation.PaymentStatus = ParticipantPaymentStatus.Refunded;

            var admins = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
            var user   = await _userManager.FindByIdAsync(userId);
            foreach (var admin in admins)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId  = admin.Id,
                    Message = _T["↩️ طلب استرجاع عربون — المصنّع «{0}» انسحب من حملة «{1}». يرجى استرجاع العربون عبر CliQ.", user?.FullName ?? userId, campaign.Title].Value,
                    Link    = $"/GroupBuying/Details/{campaign.Id}"
                });
            }

            campaign.CurrentQuantity -= participation.Quantity;
            if (campaign.CurrentQuantity < 0) campaign.CurrentQuantity = 0;

            await _db.SaveChangesAsync();
            TempData["Success"] = _T["تم انسحابك. سيتم استرجاع العربون إليك خلال 3 أيام عمل."].Value;
            return RedirectToAction(nameof(Index));
        }

        campaign.CurrentQuantity -= participation.Quantity;
        if (campaign.CurrentQuantity < 0) campaign.CurrentQuantity = 0;

        _db.CampaignParticipants.Remove(participation);
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم انسحابك من الحملة."].Value;
        return RedirectToAction(nameof(Index));
    }

    // POST: /GroupBuying/SubmitDeposit  — رفع إيصال العربون (نصف المبلغ)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> SubmitDeposit(int campaignId, IFormFile receiptImage)
    {
        var userId = _userManager.GetUserId(User)!;

        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null) return NotFound();

        var participant = campaign.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            TempData["Error"] = _T["أنت لست مشاركاً في هذه الحملة."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (participant.PaymentStatus != ParticipantPaymentStatus.NotPaid)
        {
            TempData["Error"] = _T["تم رفع الإيصال مسبقاً."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (receiptImage == null || receiptImage.Length == 0)
        {
            TempData["Error"] = _T["يرجى رفع صورة إيصال التحويل."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        var ext     = Path.GetExtension(receiptImage.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
        {
            TempData["Error"] = _T["صيغة الملف غير مدعومة. يُسمح بـ JPG، PNG، PDF فقط."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "receipts");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"deposit_{campaignId}_{participant.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
            await receiptImage.CopyToAsync(stream);

        var depositAmount = Math.Round(campaign.GroupPrice * participant.Quantity / 2, 2);
        participant.DepositReceiptPath = $"/uploads/receipts/{fileName}";
        participant.DepositAmount      = depositAmount;
        participant.PaymentStatus      = ParticipantPaymentStatus.DepositUnderReview;

        // إشعار للمدير
        var admins = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
        foreach (var admin in admins)
        {
            _db.Notifications.Add(new Notification
            {
                UserId  = admin.Id,
                Message = _T["💳 إيصال عربون جديد — حملة «{0}» — المبلغ: {1:N2} د.أ. يرجى المراجعة.", campaign.Title, depositAmount].Value,
                Link    = $"/GroupBuying/Details/{campaignId}"
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = _T["تم رفع إيصال العربون. سيتم التحقق منه خلال 24 ساعة."].Value;
        return RedirectToAction(nameof(Details), new { id = campaignId });
    }

    // POST: /GroupBuying/SubmitRemainingPayment  — رفع إيصال المبلغ المتبقي بعد اكتمال النصاب
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> SubmitRemainingPayment(int campaignId, IFormFile receiptImage)
    {
        var userId = _userManager.GetUserId(User)!;

        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign == null) return NotFound();

        var participant = campaign.Participants.FirstOrDefault(p => p.UserId == userId);
        if (participant == null)
        {
            TempData["Error"] = _T["أنت لست مشاركاً في هذه الحملة."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (participant.PaymentStatus != ParticipantPaymentStatus.DepositPaid)
        {
            TempData["Error"] = _T["يجب أن يكون العربون مؤكداً قبل دفع المبلغ المتبقي."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        if (receiptImage == null || receiptImage.Length == 0)
        {
            TempData["Error"] = _T["يرجى رفع صورة إيصال التحويل."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        var ext     = Path.GetExtension(receiptImage.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
        {
            TempData["Error"] = _T["صيغة الملف غير مدعومة. يُسمح بـ JPG، PNG، PDF فقط."].Value;
            return RedirectToAction(nameof(Details), new { id = campaignId });
        }

        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "receipts");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"remaining_{campaignId}_{participant.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
            await receiptImage.CopyToAsync(stream);

        var totalAmount     = campaign.GroupPrice * participant.Quantity;
        var remainingAmount = Math.Round(totalAmount - (participant.DepositAmount ?? 0), 2);
        participant.RemainingReceiptPath = $"/uploads/receipts/{fileName}";
        participant.RemainingAmount      = remainingAmount;
        participant.PaymentStatus        = ParticipantPaymentStatus.FullUnderReview;

        // إشعار للمدير
        var admins = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
        foreach (var admin in admins)
        {
            _db.Notifications.Add(new Notification
            {
                UserId  = admin.Id,
                Message = _T["💳 إيصال دفع كامل — حملة «{0}» — المبلغ المتبقي: {1:N2} د.أ. يرجى المراجعة.", campaign.Title, remainingAmount].Value,
                Link    = $"/GroupBuying/Details/{campaignId}"
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = _T["تم رفع إيصال الدفع المتبقي. سيتم التحقق منه خلال 24 ساعة."].Value;
        return RedirectToAction(nameof(Details), new { id = campaignId });
    }

    // POST: /GroupBuying/ApproveDeposit  (Admin)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> ApproveDeposit(int participantId)
    {
        var participant = await _db.CampaignParticipants
            .Include(p => p.Campaign)
            .FirstOrDefaultAsync(p => p.Id == participantId);

        if (participant == null) return NotFound();

        participant.PaymentStatus = ParticipantPaymentStatus.DepositPaid;
        participant.DepositPaidAt = DateTime.UtcNow;

        _db.Notifications.Add(new Notification
        {
            UserId  = participant.UserId,
            Message = _T["✅ تم قبول عربونك ({0:N2} د.أ) لحملة «{1}». مشاركتك مؤكدة!", participant.DepositAmount ?? 0, participant.Campaign!.Title].Value,
            Link    = $"/GroupBuying/Details/{participant.CampaignId}"
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = _T["تم قبول العربون بنجاح."].Value;
        return RedirectToAction(nameof(Details), new { id = participant.CampaignId });
    }

    // POST: /GroupBuying/ApproveFullPayment  (Admin)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> ApproveFullPayment(int participantId)
    {
        var participant = await _db.CampaignParticipants
            .Include(p => p.Campaign)
            .FirstOrDefaultAsync(p => p.Id == participantId);

        if (participant == null) return NotFound();

        participant.PaymentStatus = ParticipantPaymentStatus.FullPaid;
        participant.FullPaidAt    = DateTime.UtcNow;

        _db.Notifications.Add(new Notification
        {
            UserId  = participant.UserId,
            Message = _T["✅ تم تأكيد دفعتك الكاملة لحملة «{0}». شكراً لك!", participant.Campaign!.Title].Value,
            Link    = $"/GroupBuying/Details/{participant.CampaignId}"
        });

        // هل دفع الجميع؟ → تأكيد الحملة تلقائياً
        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == participant.CampaignId);

        if (campaign != null)
        {
            var allPaid = campaign.Participants.All(p =>
                p.PaymentStatus == ParticipantPaymentStatus.FullPaid
                || p.PaymentStatus == ParticipantPaymentStatus.Refunded);

            if (allPaid)
            {
                campaign.Status = CampaignStatus.Confirmed;

                var admins = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
                foreach (var admin in admins)
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId  = admin.Id,
                        Message = _T["✅ حملة مؤكدة — اكتملت جميع المدفوعات لحملة «{0}». يمكنك إصدار أمر الشراء.", campaign.Title].Value,
                        Link    = $"/GroupBuying/Details/{campaign.Id}"
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = _T["تم قبول الدفع الكامل بنجاح."].Value;
        return RedirectToAction(nameof(Details), new { id = participant.CampaignId });
    }

    // POST: /GroupBuying/RejectParticipantPayment  (Admin)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> RejectParticipantPayment(int participantId, string? reviewNote)
    {
        var participant = await _db.CampaignParticipants
            .Include(p => p.Campaign)
            .FirstOrDefaultAsync(p => p.Id == participantId);

        if (participant == null) return NotFound();

        bool wasDeposit = participant.PaymentStatus == ParticipantPaymentStatus.DepositUnderReview;
        bool wasFull    = participant.PaymentStatus == ParticipantPaymentStatus.FullUnderReview;

        if (wasDeposit)
        {
            participant.DepositReceiptPath = null;
            participant.DepositAmount      = null;
            participant.PaymentStatus      = ParticipantPaymentStatus.NotPaid;
        }
        else if (wasFull)
        {
            participant.RemainingReceiptPath = null;
            participant.RemainingAmount      = null;
            participant.PaymentStatus        = ParticipantPaymentStatus.DepositPaid;
        }

        participant.PaymentReviewNote = reviewNote;

        _db.Notifications.Add(new Notification
        {
            UserId  = participant.UserId,
            Message = _T["❌ تم رفض إيصال {0} لحملة «{1}». السبب: {2}. يرجى رفع الإيصال مجدداً.",
                wasDeposit ? _T["العربون"].Value : _T["الدفع"].Value,
                participant.Campaign!.Title,
                reviewNote ?? _T["يرجى التواصل مع الإدارة"].Value].Value,
            Link    = $"/GroupBuying/Details/{participant.CampaignId}"
        });

        await _db.SaveChangesAsync();
        TempData["Error"] = _T["تم رفض الإيصال وإشعار المشارك."].Value;
        return RedirectToAction(nameof(Details), new { id = participant.CampaignId });
    }

    // POST: /GroupBuying/RequestPayment/5  (Admin) — إشعار للمشاركين لدفع المبلغ المتبقي
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
            TempData["Error"] = _T["لم يكتمل النصاب بعد ({0}/{1}).", campaign.CurrentQuantity, campaign.MinQuantity].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        campaign.Status = CampaignStatus.AwaitingPayment;

        // إشعار للمشاركين الذين أكملوا العربون
        var targets = campaign.Participants
            .Where(p => p.PaymentStatus == ParticipantPaymentStatus.DepositPaid);

        foreach (var p in targets)
        {
            var remaining = Math.Round(campaign.GroupPrice * p.Quantity - (p.DepositAmount ?? 0), 2);
            _db.Notifications.Add(new Notification
            {
                UserId  = p.UserId,
                Message = _T["💳 اكتمل النصاب — يرجى دفع المبلغ المتبقي ({0:N2} د.أ) لحملة «{1}».", remaining, campaign.Title].Value,
                Link    = $"/GroupBuying/Details/{campaign.Id}"
            });
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = _T["تم إرسال إشعار الدفع لـ {0} مشارك.", targets.Count()].Value;
        return RedirectToAction(nameof(Details), new { id });
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

            model.Status          = CampaignStatus.Active;
            model.CurrentQuantity = 0;

            _db.GroupBuyingCampaigns.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = _T["تم إنشاء الحملة بنجاح."].Value;
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
            TempData["Error"] = _T["عنوان الحملة مطلوب."].Value;
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
            TempData["Success"] = _T["تم تحديث الحملة بنجاح."].Value;
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

        TempData["Success"] = _T["تم تحديث حالة الحملة."].Value;
        return RedirectToAction(nameof(Details), new { id });
    }
}
