using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IStringLocalizer<SharedResource> _T;

    public ProductsController(ApplicationDbContext db, IStringLocalizer<SharedResource> T)
    {
        _db = db;
        _T = T;
    }

    // GET: /Products
    public async Task<IActionResult> Index(string? search, string? category, string? seller)
    {
        var query = _db.Products
            .Include(p => p.SellerUser)
            .Include(p => p.Manufacturer)
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
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        return View(product);
    }
}
