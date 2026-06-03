using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

[Authorize]
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public PaymentsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IConfiguration config)
    {
        _db = db;
        _userManager = userManager;
        _config = config;
        StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
    }

    // GET: /Payments/Checkout/5  (requestId)
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

        // العرض المقبول
        var accepted = request.Quotations.FirstOrDefault(q => q.Status == QuotationStatus.Accepted);
        if (accepted == null)
        {
            TempData["Error"] = "لا يوجد عرض مقبول لهذا الطلب.";
            return RedirectToAction("Details", "Requests", new { id = requestId });
        }

        ViewBag.Request  = request;
        ViewBag.Quotation = accepted;
        ViewBag.PublishableKey = _config["Stripe:PublishableKey"];
        return View();
    }

    // POST: /Payments/CreateCheckoutSession
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCheckoutSession(int requestId)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ManufacturingRequests
            .Include(r => r.Quotations).ThenInclude(q => q.Manufacturer)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CustomerId == userId);

        if (request == null) return NotFound();

        var accepted = request.Quotations.FirstOrDefault(q => q.Status == QuotationStatus.Accepted);
        if (accepted == null) return BadRequest("لا يوجد عرض مقبول.");

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
                        UnitAmount  = (long)(accepted.Price * 100), // cents
                        Currency    = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name        = $"طلب تصنيع: {request.Title}",
                            Description = $"ورشة: {accepted.Manufacturer?.WorkshopName}"
                        }
                    },
                    Quantity = 1
                }
            },
            Mode       = "payment",
            SuccessUrl = $"{domain}/Payments/Success?requestId={requestId}&session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl  = $"{domain}/Payments/Cancel?requestId={requestId}",
            Metadata   = new Dictionary<string, string>
            {
                { "requestId",   requestId.ToString() },
                { "quotationId", accepted.Id.ToString() },
                { "userId",      userId }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        // احفظ session ID
        request.StripeSessionId  = session.Id;
        request.PaymentStatus    = PaymentStatus.Pending;
        await _db.SaveChangesAsync();

        return Redirect(session.Url);
    }

    // GET: /Payments/Success
    [HttpGet]
    public async Task<IActionResult> Success(int requestId, string session_id)
    {
        var userId = _userManager.GetUserId(User)!;

        var request = await _db.ManufacturingRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.CustomerId == userId);

        if (request == null) return NotFound();

        // تحقق من Stripe
        var service = new SessionService();
        var session = await service.GetAsync(session_id);

        if (session.PaymentStatus == "paid")
        {
            request.PaymentStatus = PaymentStatus.Paid;
            request.PaidAmount    = (decimal)(session.AmountTotal ?? 0) / 100;
            request.PaidAt        = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            TempData["Success"] = "✅ تم الدفع بنجاح! سيتواصل معك المصنّع قريباً.";
        }
        else
        {
            TempData["Error"] = "لم يكتمل الدفع. يرجى المحاولة مجدداً.";
        }

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
