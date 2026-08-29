using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.Services;

namespace DMP.Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileService _fileService;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;
    private readonly CartService _cart;

    public OrdersController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileService fileService,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T,
        CartService cart)
    {
        _db = db;
        _userManager = userManager;
        _fileService = fileService;
        _T = T;
        _cart = cart;
    }

    // GET: /Orders — سجل طلباتي
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return View(orders);
    }

    // GET: /Orders/Checkout — ملخص الطلب مع CliQ
    public async Task<IActionResult> Checkout()
    {
        var items = await _cart.GetItemsAsync();
        if (!items.Any())
        {
            TempData["Error"] = _T["سلتك فارغة."].Value;
            return RedirectToAction("Index", "Cart");
        }

        var invalid = items.FirstOrDefault(c => c.Product == null
                                             || !c.Product.IsActive
                                             || c.Product.Stock < c.Quantity);
        if (invalid != null)
        {
            TempData["Error"] = _T["المنتج \"{0}\" غير متوفر بالكمية المطلوبة.", invalid.Product?.Name ?? "—"].Value;
            return RedirectToAction("Index", "Cart");
        }

        ViewBag.Total = await _cart.GetTotalAsync();
        ViewBag.Count = items.Sum(c => c.Quantity);
        return View();
    }

    // POST: /Orders/PlaceOrder — إنشاء الطلب وتأكيده ثم التوجه للدفع
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(string contactPhone, string shippingAddress, string notes)
    {
        var userId = _userManager.GetUserId(User)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var items = (await _cart.GetItemsAsync()).Where(c => c.Product != null).ToList();
        if (!items.Any())
        {
            TempData["Error"] = _T["سلتك فارغة."].Value;
            return RedirectToAction("Index", "Cart");
        }

        // التحقق من توفر المنتجات
        foreach (var item in items)
        {
            var prod = item.Product!;
            if (!prod.IsActive || prod.Stock < item.Quantity)
            {
                TempData["Error"] = _T["المنتج \"{0}\" غير متوفر بالكمية المطلوبة.", prod.Name].Value;
                return RedirectToAction("Index", "Cart");
            }
        }

        // إنشاء رقم الطلب
        var orderCount = await _db.Orders.CountAsync();
        var orderNumber = $"JM-{DateTime.UtcNow:yyyyMMdd}-{orderCount + 1}";
        while (await _db.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
        {
            orderCount++;
            orderNumber = $"JM-{DateTime.UtcNow:yyyyMMdd}-{orderCount + 1}";
        }

        var order = new Order
        {
            OrderNumber     = orderNumber,
            CustomerId      = userId,
            CustomerName    = user.FullName,
            CustomerEmail   = user.Email ?? "",
            ContactPhone    = contactPhone ?? "",
            ShippingAddress = shippingAddress,
            Notes           = notes,
            TotalAmount     = items.Sum(i => (i.Product!.Price) * i.Quantity),
            Status          = OrderStatus.Pending,
            CreatedAt       = DateTime.UtcNow
        };
        _db.Orders.Add(order);

        foreach (var item in items)
        {
            _db.OrderItems.Add(new OrderItem
            {
                Order       = order,
                ProductId   = item.ProductId,
                ProductName = item.Product!.Name,
                ImagePath   = item.Product.ImagePath,
                UnitPrice   = item.Product.Price,
                Quantity    = item.Quantity
            });

            // خصم المخزون
            item.Product.Stock = Math.Max(0, item.Product.Stock - item.Quantity);
        }

        await _db.SaveChangesAsync();

        // إشعار للمدير بطلب جديد
        var admin = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
        foreach (var adminUser in admin)
        {
            _db.Notifications.Add(new Notification
            {
                UserId    = adminUser.Id,
                Message   = _T["طلب جديد رقم #{0} — {1}", orderNumber, _T["بانتظار الدفع"].Value].Value,
                Link      = $"/Admin/Orders",
                CreatedAt = DateTime.UtcNow
            });
        }

        // مسح السلة
        var cartItems = await _cart.GetItemsAsync();
        if (cartItems.Count > 0)
        {
            _db.CartItems.RemoveRange(cartItems);
        }
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Payment), new { id = order.Id });
    }

    // GET: /Orders/Payment/5 — صفحة CliQ + رفع الإيصال
    public async Task<IActionResult> Payment(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);
        if (order == null) return NotFound();

        if (order.Status == OrderStatus.Paid)
        {
            TempData["Success"] = _T["تم تأكيد دفع هذا الطلب."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }
        if (order.Status == OrderStatus.UnderReview)
        {
            TempData["Error"] = _T["إيصال الدفع قيد المراجعة من الإدارة."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        return View(order);
    }

    // POST: /Orders/SubmitReceipt — رفع إيصال الدفع
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitReceipt(int id, IFormFile receiptImage)
    {
        var userId = _userManager.GetUserId(User)!;
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);
        if (order == null) return NotFound();

        if (order.Status == OrderStatus.Paid || order.Status == OrderStatus.UnderReview)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        if (receiptImage == null || receiptImage.Length == 0)
        {
            TempData["Error"] = _T["يرجى رفع صورة إيصال الدفع."].Value;
            return RedirectToAction(nameof(Payment), new { id });
        }

        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        var ext = Path.GetExtension(receiptImage.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
        {
            TempData["Error"] = _T["صيغة الملف غير مدعومة. يُسمح بـ JPG، PNG، PDF فقط."].Value;
            return RedirectToAction(nameof(Payment), new { id });
        }

        var saved = await _fileService.SaveFileAsync(receiptImage, "orders");
        if (saved == null)
        {
            TempData["Error"] = _T["تعذر حفظ الإيصال. تحقق من حجم وصيغة الملف."].Value;
            return RedirectToAction(nameof(Payment), new { id });
        }

        order.PaymentReceiptPath = saved;
        order.Status             = OrderStatus.UnderReview;
        await _db.SaveChangesAsync();

        var admin = await _userManager.GetUsersInRoleAsync(SeedData.AdminRole);
        foreach (var adminUser in admin)
        {
            _db.Notifications.Add(new Notification
            {
                UserId    = adminUser.Id,
                Message   = _T["إيصال دفع جديد بانتظار مراجعتك — طلب #{0}", order.OrderNumber].Value,
                Link      = $"/Admin/Orders",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم رفع إيصال الدفع بنجاح. سيتم مراجعته وتأكيده خلال 24 ساعة."].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Orders/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);
        if (order == null) return NotFound();

        return View(order);
    }
}