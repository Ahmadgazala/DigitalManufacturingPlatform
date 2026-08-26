using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Helpers;
using DMP.Web.Models;
using DMP.Web.Services;
using DMP.Web.ViewModels;

namespace DMP.Web.Controllers;

public class ManufacturersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileService _fileService;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;

    public ManufacturersController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileService fileService,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T)
    {
        _db = db;
        _userManager = userManager;
        _fileService = fileService;
        _T = T;
    }

    // GET: /Manufacturers
    public async Task<IActionResult> Index(string? searchTerm, MachineCategory? category, string? city, int page = 1)
    {
        const int pageSize = 9;

        var query = _db.Manufacturers
            .Include(m => m.Machines)
            .Include(m => m.Reviews)
            .Where(m => m.IsApproved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(m =>
                m.WorkshopName.Contains(searchTerm) ||
                m.City.Contains(searchTerm) ||
                (m.Bio != null && m.Bio.Contains(searchTerm)));

        if (category.HasValue)
            query = query.Where(m => m.Machines.Any(mc => mc.Category == category.Value));

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(m => m.City == city);

        query = query.OrderByDescending(m => m.CreatedAt);

        var manufacturers = await PaginatedList<Manufacturer>.CreateAsync(query, page, pageSize);

        var cities = await _db.Manufacturers
            .Where(m => m.IsApproved)
            .Select(m => m.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        ViewBag.PageIndex  = manufacturers.PageIndex;
        ViewBag.TotalPages = manufacturers.TotalPages;
        ViewBag.TotalCount = manufacturers.TotalCount;

        var vm = new DirectoryViewModel
        {
            Manufacturers = manufacturers.Items,
            SearchTerm    = searchTerm,
            Category      = category,
            City          = city,
            Cities        = cities
        };

        return View(vm);
    }

    // GET: /Manufacturers/Dashboard
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> Dashboard()
    {
        var userId = _userManager.GetUserId(User)!;

        var manufacturer = await _db.Manufacturers
            .Include(m => m.Machines)
            .Include(m => m.WorkshopPhotos)
            .Include(m => m.Reviews)
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (manufacturer == null)
        {
            TempData["Info"] = _T["يرجى إكمال ملف الورشة أولاً."].Value;
            return RedirectToAction(nameof(Edit));
        }

        var manufacturerCategories = manufacturer.Machines.Select(m => m.Category).Distinct().ToList();

        var recentRequests = await _db.ManufacturingRequests
            .Include(r => r.Customer)
            .Where(r => r.Status == RequestStatus.Open && manufacturerCategories.Contains(r.Category))
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        var quotations = await _db.Quotations
            .Include(q => q.Request)
            .Where(q => q.ManufacturerId == manufacturer.Id)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        var activeOrders = await _db.ManufacturingRequests
            .Where(r => r.ManufacturerId == manufacturer.Id && r.Status == RequestStatus.InProgress)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var vm = new ManufacturerDashboardViewModel
        {
            Manufacturer = manufacturer,
            OpenRequestsCount = recentRequests.Count,
            PendingQuotationsCount = quotations.Count(q => q.Status == QuotationStatus.Pending),
            AcceptedQuotationsCount = quotations.Count(q => q.Status == QuotationStatus.Accepted),
            CompletedOrdersCount = await _db.ManufacturingRequests
                .CountAsync(r => r.ManufacturerId == manufacturer.Id && r.Status == RequestStatus.Completed),
            RecentRequests   = recentRequests,
            RecentQuotations = quotations.Take(5).ToList(),
            ActiveOrders     = activeOrders
        };

        return View(vm);
    }

    // GET: /Manufacturers/Map
    public async Task<IActionResult> Map(MachineCategory? category, string? city)
    {
        var query = _db.Manufacturers
            .Include(m => m.Machines)
            .Include(m => m.Reviews)
            .Where(m => m.IsApproved && m.Latitude.HasValue && m.Longitude.HasValue)
            .AsQueryable();

        if (category.HasValue)
            query = query.Where(m => m.Machines.Any(mc => mc.Category == category));

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(m => m.City == city);

        var manufacturers = await query.ToListAsync();

        var cities = await _db.Manufacturers
            .Where(m => m.IsApproved && !string.IsNullOrEmpty(m.City))
            .Select(m => m.City)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        ViewBag.Cities    = cities;
        ViewBag.Category  = category;
        ViewBag.City      = city;
        ViewBag.AllCount  = await _db.Manufacturers.CountAsync(m => m.IsApproved);

        return View(manufacturers);
    }

    // GET: /Manufacturers/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var manufacturer = await _db.Manufacturers
            .Include(m => m.User)
            .Include(m => m.Machines)
            .Include(m => m.WorkshopPhotos)
            .Include(m => m.Reviews)
                .ThenInclude(r => r.Customer)
            .Include(m => m.PortfolioItems)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (manufacturer == null)
            return NotFound();

        // غير معتمدة — يسمح فقط للـ Admin بالمشاهدة
        if (!manufacturer.IsApproved && !User.IsInRole("Admin"))
            return NotFound();

        return View(manufacturer);
    }

    // GET: /Manufacturers/Edit
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> Edit()
    {
        var userId = _userManager.GetUserId(User)!;
        var manufacturer = await _db.Manufacturers.FirstOrDefaultAsync(m => m.UserId == userId);

        ManufacturerFormViewModel vm;

        if (manufacturer == null)
        {
            vm = new ManufacturerFormViewModel();
        }
        else
        {
            vm = new ManufacturerFormViewModel
            {
                Id = manufacturer.Id,
                WorkshopName = manufacturer.WorkshopName,
                Bio = manufacturer.Bio,
                City = manufacturer.City,
                Address = manufacturer.Address,
                Latitude = manufacturer.Latitude,
                Longitude = manufacturer.Longitude,
                ContactPhone = manufacturer.ContactPhone,
                ContactEmail = manufacturer.ContactEmail,
                WorkingHours = manufacturer.WorkingHours,
                Delivery = manufacturer.Delivery,
                ProductionCapacity = manufacturer.ProductionCapacity,
                ExistingProfilePhotoPath = manufacturer.ProfilePhotoPath,
                ExistingCoverPhotoPath = manufacturer.CoverPhotoPath
            };
        }

        return View(vm);
    }

    // POST: /Manufacturers/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> Edit(ManufacturerFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var userId = _userManager.GetUserId(User)!;
        var manufacturer = await _db.Manufacturers.FirstOrDefaultAsync(m => m.UserId == userId);

        try
        {
            if (manufacturer == null)
            {
                manufacturer = new Manufacturer { UserId = userId };
                _db.Manufacturers.Add(manufacturer);
            }

            manufacturer.WorkshopName = vm.WorkshopName;
            manufacturer.Bio = vm.Bio;
            manufacturer.City = vm.City;
            manufacturer.Address = vm.Address;
            manufacturer.Latitude = vm.Latitude;
            manufacturer.Longitude = vm.Longitude;
            manufacturer.ContactPhone = vm.ContactPhone;
            manufacturer.ContactEmail = vm.ContactEmail;
            manufacturer.WorkingHours = vm.WorkingHours;
            manufacturer.Delivery = vm.Delivery;
            manufacturer.ProductionCapacity = vm.ProductionCapacity;

            if (vm.ProfilePhoto != null)
            {
                _fileService.Delete(manufacturer.ProfilePhotoPath);
                manufacturer.ProfilePhotoPath = await _fileService.SaveImageAsync(vm.ProfilePhoto, "profiles");
            }

            if (vm.CoverPhoto != null)
            {
                _fileService.Delete(manufacturer.CoverPhotoPath);
                manufacturer.CoverPhotoPath = await _fileService.SaveImageAsync(vm.CoverPhoto, "covers");
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = _T["تم حفظ بيانات الورشة بنجاح."].Value;
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(vm);
        }
    }

    // POST: /Manufacturers/UploadPhoto
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> UploadPhoto(IFormFile? photo)
    {
        var userId = _userManager.GetUserId(User)!;
        var manufacturer = await _db.Manufacturers.FirstOrDefaultAsync(m => m.UserId == userId);

        if (manufacturer == null)
        {
            TempData["Error"] = _T["يرجى إنشاء ملف الورشة أولاً."].Value;
            return RedirectToAction(nameof(Edit));
        }

        if (photo == null || photo.Length == 0)
        {
            TempData["Error"] = _T["لم يتم اختيار صورة."].Value;
            return RedirectToAction(nameof(Dashboard));
        }

        try
        {
            var path = await _fileService.SaveImageAsync(photo, "workshop");
            if (path != null)
            {
                _db.WorkshopPhotos.Add(new WorkshopPhoto
                {
                    ManufacturerId = manufacturer.Id,
                    FilePath = path
                });
                await _db.SaveChangesAsync();
                TempData["Success"] = _T["تم رفع الصورة بنجاح."].Value;
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Dashboard));
    }

    // POST: /Manufacturers/DeletePhoto/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> DeletePhoto(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var manufacturer = await _db.Manufacturers.FirstOrDefaultAsync(m => m.UserId == userId);

        if (manufacturer == null)
            return Forbid();

        var photo = await _db.WorkshopPhotos
            .FirstOrDefaultAsync(p => p.Id == id && p.ManufacturerId == manufacturer.Id);

        if (photo == null)
            return NotFound();

        _fileService.Delete(photo.FilePath);
        _db.WorkshopPhotos.Remove(photo);
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم حذف الصورة."].Value;
        return RedirectToAction(nameof(Dashboard));
    }

    // POST: /Manufacturers/AddReview/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> AddReview(int id, int rating, string? comment)
    {
        var userId = _userManager.GetUserId(User)!;

        // منع صاحب الملف من التقييم
        var isOwner = await _db.Manufacturers.AnyAsync(m => m.Id == id && m.UserId == userId);
        if (isOwner)
        {
            TempData["Error"] = _T["لا يمكنك تقييم ورشتك الخاصة."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // منع التقييم المتكرر
        var alreadyReviewed = await _db.Reviews.AnyAsync(r => r.ManufacturerId == id && r.CustomerUserId == userId);
        if (alreadyReviewed)
        {
            TempData["Error"] = _T["لقد قيّمت هذه الورشة من قبل."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        if (rating < 1 || rating > 5)
        {
            TempData["Error"] = _T["التقييم يجب أن يكون بين 1 و 5."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await _userManager.GetUserAsync(User);

        _db.Reviews.Add(new Review
        {
            ManufacturerId = id,
            CustomerUserId = userId,
            CustomerName = user?.FullName ?? _T["مستخدم"].Value,
            Rating = rating,
            Comment = comment
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = _T["شكراً! تم إضافة تقييمك."].Value;
        return RedirectToAction(nameof(Details), new { id });
    }

    // ========== Manufacturer Products (Marketplace) ==========

    // GET: /Manufacturers/MyProducts
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> MyProducts()
    {
        var userId = _userManager.GetUserId(User)!;
        var manufacturer = await _db.Manufacturers
            .FirstOrDefaultAsync(m => m.UserId == userId);

        if (manufacturer == null)
        {
            TempData["Info"] = _T["يرجى إكمال ملف الورشة أولاً."].Value;
            return RedirectToAction(nameof(Edit));
        }

        var products = await _db.Products
            .Where(p => p.ManufacturerId == manufacturer.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(products);
    }

    // GET: /Manufacturers/CreateProduct
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> CreateProduct()
    {
        var userId = _userManager.GetUserId(User)!;
        var manufacturer = await _db.Manufacturers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (manufacturer == null) return RedirectToAction(nameof(Edit));

        return View();
    }

    // POST: /Manufacturers/CreateProduct
    [HttpPost]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(Product model, IFormFile? imageFile)
    {
        var userId = _userManager.GetUserId(User)!;
        var manufacturer = await _db.Manufacturers.FirstOrDefaultAsync(m => m.UserId == userId);
        if (manufacturer == null) return RedirectToAction(nameof(Edit));

        ModelState.Remove("SellerUserId");
        ModelState.Remove("ManufacturerId");

        if (!ModelState.IsValid)
            return View(model);

        model.SellerType = SellerType.Manufacturer;
        model.SellerUserId = userId;
        model.ManufacturerId = manufacturer.Id;
        model.CreatedAt = DateTime.UtcNow;

        if (imageFile != null)
            model.ImagePath = await _fileService.SaveImageAsync(imageFile, "products");

        _db.Products.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم إضافة المنتج \"{0}\" بنجاح.", model.Name].Value;
        return RedirectToAction(nameof(MyProducts));
    }

    // GET: /Manufacturers/EditProduct/5
    [Authorize(Roles = SeedData.ManufacturerRole)]
    public async Task<IActionResult> EditProduct(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.SellerUserId == userId);
        if (product == null) return NotFound();
        return View(product);
    }

    // POST: /Manufacturers/EditProduct/5
    [HttpPost]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(int id, Product model, IFormFile? imageFile)
    {
        var userId = _userManager.GetUserId(User)!;
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.SellerUserId == userId);
        if (product == null) return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        product.Name = model.Name;
        product.Description = model.Description;
        product.Price = model.Price;
        product.Category = model.Category;
        product.Stock = model.Stock;
        product.IsActive = model.IsActive;

        if (imageFile != null)
        {
            if (!string.IsNullOrEmpty(product.ImagePath))
                _fileService.Delete(product.ImagePath);
            product.ImagePath = await _fileService.SaveImageAsync(imageFile, "products");
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم تعديل المنتج \"{0}\" بنجاح.", product.Name].Value;
        return RedirectToAction(nameof(MyProducts));
    }

    // POST: /Manufacturers/DeleteProduct/5
    [HttpPost]
    [Authorize(Roles = SeedData.ManufacturerRole)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.SellerUserId == userId);
        if (product == null) return NotFound();

        if (!string.IsNullOrEmpty(product.ImagePath))
            _fileService.Delete(product.ImagePath);

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم حذف المنتج \"{0}\" بنجاح.", product.Name].Value;
        return RedirectToAction(nameof(MyProducts));
    }
}
