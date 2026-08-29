using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _T;

    public ProductsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IStringLocalizer<SharedResource> T)
    {
        _db = db;
        _userManager = userManager;
        _T = T;
    }

    // GET: /Products
    public async Task<IActionResult> Index(string? search, string? category, string? seller)
    {
        var query = _db.Products
            .Include(p => p.SellerUser)
            .Include(p => p.Manufacturer)
            .Include(p => p.Reviews)
            .Where(p => p.IsActive)
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

    // GET: /Products/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products
            .Include(p => p.SellerUser)
            .Include(p => p.Manufacturer)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        return View(product);
    }

    // POST: /Products/AddReview/5
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(int id, int rating, string? comment)
    {
        var userId = _userManager.GetUserId(User)!;

        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        // منع صاحب المنتج من تقييم منتجه
        if (product.SellerUserId == userId)
        {
            TempData["Error"] = _T["لا يمكنك تقييم منتجك الخاص."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        // منع التقييم المتكرر
        var alreadyReviewed = await _db.ProductReviews.AnyAsync(r => r.ProductId == id && r.CustomerUserId == userId);
        if (alreadyReviewed)
        {
            TempData["Error"] = _T["لقد قيّمت هذا المنتج من قبل."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        if (rating < 1 || rating > 5)
        {
            TempData["Error"] = _T["التقييم يجب أن يكون بين 1 و 5."].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await _userManager.GetUserAsync(User);

        _db.ProductReviews.Add(new ProductReview
        {
            ProductId      = id,
            CustomerUserId = userId,
            CustomerName   = user?.FullName ?? _T["مستخدم"].Value,
            Rating         = rating,
            Comment        = comment
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = _T["شكراً! تم إضافة تقييمك."].Value;
        return RedirectToAction(nameof(Details), new { id });
    }
}
