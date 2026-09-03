using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.Services;

namespace DMP.Web.Controllers;

[Authorize(Roles = SeedData.AdminRole)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileService _fileService;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        IFileService fileService,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T)
    {
        _db = db;
        _userManager = userManager;
        _fileService = fileService;
        _T = T;
    }

    // GET: /Admin
    public async Task<IActionResult> Index()
    {
        ViewBag.TotalManufacturers = await _db.Manufacturers.CountAsync();
        ViewBag.PendingManufacturers = await _db.Manufacturers.CountAsync(m => !m.IsApproved);
        ViewBag.TotalRequests = await _db.ManufacturingRequests.CountAsync();
        ViewBag.PendingRequests = await _db.ManufacturingRequests.CountAsync(r => !r.IsApproved);
        ViewBag.TotalUsers = await _userManager.Users.CountAsync();
        ViewBag.TotalSuppliers = await _db.Suppliers.CountAsync();
        ViewBag.TotalCampaigns = await _db.GroupBuyingCampaigns.CountAsync(c => c.Status == CampaignStatus.Active);
        ViewBag.TotalProducts = await _db.Products.CountAsync();
        ViewBag.TotalOrders = await _db.Orders.CountAsync();
        ViewBag.PendingOrders = await _db.Orders.CountAsync(o =>
            o.Status == OrderStatus.Pending || o.Status == OrderStatus.UnderReview);

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
    public async Task<IActionResult> Requests(string? status, string? search, string? approval)
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

        if (approval == "pending")
            query = query.Where(r => !r.IsApproved);
        else if (approval == "approved")
            query = query.Where(r => r.IsApproved);

        ViewBag.StatusFilter = status;
        ViewBag.Search       = search;
        ViewBag.ApprovalFilter = approval;

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return View(requests);
    }

    // GET: /Admin/GroupBuying  →  صفحة إدارة الحملات
    public async Task<IActionResult> GroupBuying()
    {
        var campaigns = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(campaigns);
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

    // POST: /Admin/ApproveRequest/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int id)
    {
        var request = await _db.ManufacturingRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.IsApproved = true;
        await _db.SaveChangesAsync();

        _db.Notifications.Add(new Notification
        {
            UserId  = request.CustomerId,
            Message = _T["تم اعتماد طلبك \"{0}\". يمكنك الآن استقبال عروض الأسعار من المصنّعين.", request.Title].Value,
            Link    = $"/Requests/Details/{request.Id}",
            IsRead  = false
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم اعتماد الطلب بنجاح."].Value;
        return RedirectToAction(nameof(Requests));
    }

    // POST: /Admin/RejectRequest/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int id)
    {
        var request = await _db.ManufacturingRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.IsApproved = false;
        await _db.SaveChangesAsync();

        _db.Notifications.Add(new Notification
        {
            UserId  = request.CustomerId,
            Message = _T["لم يُعتمد طلبك \"{0}\". يرجى مراجعة البيانات وإعادة الإرسال.", request.Title].Value,
            Link    = $"/Requests/Details/{request.Id}",
            IsRead  = false
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم رفض الطلب."].Value;
        return RedirectToAction(nameof(Requests));
    }

    // POST: /Admin/DeleteRequest/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        var request = await _db.ManufacturingRequests
            .Include(r => r.Files)
            .Include(r => r.Quotations)
            .Include(r => r.OrderUpdates)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound();

        _db.OrderUpdates.RemoveRange(request.OrderUpdates);
        _db.Quotations.RemoveRange(request.Quotations);
        _db.RequestFiles.RemoveRange(request.Files);
        _db.ManufacturingRequests.Remove(request);
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم حذف الطلب #{0} بنجاح.", id].Value;
        return RedirectToAction(nameof(Requests));
    }

    // POST: /Admin/DeleteCampaign/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCampaign(int id)
    {
        var campaign = await _db.GroupBuyingCampaigns
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (campaign == null) return NotFound();

        _db.CampaignParticipants.RemoveRange(campaign.Participants);
        _db.GroupBuyingCampaigns.Remove(campaign);
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم حذف الحملة \"{0}\" بنجاح.", campaign.Title].Value;
        return RedirectToAction(nameof(GroupBuying));
    }

    // ========== Marketplace Products (Admin) ==========

    // GET: /Admin/Products
    public async Task<IActionResult> Products(string? search, string? category, string? seller)
    {
        var query = _db.Products
            .Include(p => p.SellerUser)
            .Include(p => p.Manufacturer)
            .Include(p => p.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.Description != null && p.Description.Contains(search)));

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<ProductCategory>(category, out var cat))
            query = query.Where(p => p.Category == cat);

        if (seller == "admin")
            query = query.Where(p => p.SellerType == SellerType.Admin);
        else if (seller == "manufacturer")
            query = query.Where(p => p.SellerType == SellerType.Manufacturer);

        ViewBag.Search = search;
        ViewBag.CategoryFilter = category;
        ViewBag.SellerFilter = seller;

        var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return View(products);
    }

    // GET: /Admin/CreateProduct
    public IActionResult CreateProduct()
    {
        return View();
    }

    // POST: /Admin/CreateProduct
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(Product model, List<IFormFile>? imageFiles)
    {
        ModelState.Remove("SellerUserId");
        ModelState.Remove("ManufacturerId");

        if (!ModelState.IsValid)
            return View(model);

        model.SellerType = SellerType.Admin;
        model.SellerUserId = _userManager.GetUserId(User);
        model.CreatedAt = DateTime.UtcNow;

        var paths = await _fileService.SaveImagesAsync(imageFiles ?? new List<IFormFile>(), "products");
        for (int i = 0; i < paths.Count; i++)
            model.Images.Add(new ProductImage { ImagePath = paths[i], IsCover = i == 0 });

        _db.Products.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم إضافة المنتج \"{0}\" بنجاح.", model.Name].Value;
        return RedirectToAction(nameof(Products));
    }

    // GET: /Admin/EditProduct/5
    public async Task<IActionResult> EditProduct(int id)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        return View(product);
    }

    // POST: /Admin/EditProduct/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(int id, Product model, List<IFormFile>? imageFiles,
        int? coverImageId, List<int>? removeImageIds)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        if (!ModelState.IsValid)
            return View(product);

        product.Name = model.Name;
        product.Description = model.Description;
        product.Price = model.Price;
        product.Category = model.Category;
        product.Stock = model.Stock;
        product.IsActive = model.IsActive;

        // حذف الصور المحددة
        if (removeImageIds != null && removeImageIds.Count > 0)
        {
            var toRemove = product.Images.Where(i => removeImageIds.Contains(i.Id)).ToList();
            foreach (var img in toRemove)
            {
                await _fileService.DeleteAsync(img.ImagePath);
                product.Images.Remove(img);
                _db.ProductImages.Remove(img);
            }
        }

        // إضافة صور جديدة
        var paths = await _fileService.SaveImagesAsync(imageFiles ?? new List<IFormFile>(), "products");
        foreach (var path in paths)
        {
            var img = new ProductImage { ImagePath = path, IsCover = false };
            product.Images.Add(img);
            _db.ProductImages.Add(img);
        }

        // اختيار صورة الغلاف
        if (coverImageId.HasValue && product.Images.Any(i => i.Id == coverImageId.Value))
        {
            foreach (var img in product.Images) img.IsCover = false;
            product.Images.First(i => i.Id == coverImageId.Value).IsCover = true;
        }
        else if (product.Images.Count > 0 && !product.Images.Any(i => i.IsCover))
        {
            product.Images.First().IsCover = true;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم تعديل المنتج \"{0}\" بنجاح.", product.Name].Value;
        return RedirectToAction(nameof(Products));
    }

    // POST: /Admin/DeleteProduct/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        if (!string.IsNullOrEmpty(product.ImagePath))
            await _fileService.DeleteAsync(product.ImagePath);
        foreach (var img in product.Images)
            await _fileService.DeleteAsync(img.ImagePath);

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم حذف المنتج \"{0}\" بنجاح.", product.Name].Value;
        return RedirectToAction(nameof(Products));
    }

    // ========== Orders (Admin) ==========

    // GET: /Admin/Orders
    public async Task<IActionResult> Orders()
    {
        var orders = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return View(orders);
    }

    // POST: /Admin/ApproveOrderPayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveOrderPayment(int id, string? note)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        order.Status             = OrderStatus.Paid;
        order.PaidAt             = DateTime.UtcNow;
        order.PaymentReviewNote  = note;

        _db.Notifications.Add(new Notification
        {
            UserId    = order.CustomerId,
            Message   = _T["تم تأكيد دفع طلبك #{0}. شكراً لثقتك بنا!", order.OrderNumber].Value,
            Link      = $"/Orders/Details/{order.Id}",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم قبول الدفع لطلب #{0}.", order.OrderNumber].Value;
        return RedirectToAction(nameof(Orders));
    }

    // POST: /Admin/RejectOrderPayment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectOrderPayment(int id, string? note)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        order.Status             = OrderStatus.Pending;
        order.PaymentReviewNote  = note;

        _db.Notifications.Add(new Notification
        {
            UserId    = order.CustomerId,
            Message   = _T["تم رفض إيصال الدفع للطلب #{0}. يرجى إعادة الدفع ورفع إيصال صحيح.", order.OrderNumber].Value
                        + (string.IsNullOrEmpty(note) ? "" : _T[" السبب: {0}", note].Value),
            Link      = $"/Orders/Payment/{order.Id}",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Error"] = _T["تم رفض الدفع للطلب #{0}.", order.OrderNumber].Value;
        return RedirectToAction(nameof(Orders));
    }

    // POST: /Admin/CompleteCashOrder — تأكيد تسليم طلب الدفع عند الاستلام ودفعه
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteCashOrder(int id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();

        if (!order.IsCashOnDelivery)
        {
            TempData["Error"] = _T["هذا الطلب ليس دفعاً عند الاستلام."].Value;
            return RedirectToAction(nameof(Orders));
        }

        order.Status = OrderStatus.Paid;
        order.PaidAt = DateTime.UtcNow;

        _db.Notifications.Add(new Notification
        {
            UserId    = order.CustomerId,
            Message   = _T["اكتمل طلبك #{0} (الدفع عند الاستلام). شكراً لثقتك بنا!", order.OrderNumber].Value,
            Link      = $"/Orders/Details/{order.Id}",
            IsRead    = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم تأكيد تسليم الطلب #{0} واستلام الدفع.", order.OrderNumber].Value;
        return RedirectToAction(nameof(Orders));
    }
}
