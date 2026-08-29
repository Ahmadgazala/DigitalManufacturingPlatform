using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Helpers;
using DMP.Web.Models;
using DMP.Web.Services;

namespace DMP.Web.Controllers;

public class SuppliersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IFileService _fileService;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;

    public SuppliersController(ApplicationDbContext db, IFileService fileService,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T)
    {
        _db = db;
        _fileService = fileService;
        _T = T;
    }

    // GET: /Suppliers
    public async Task<IActionResult> Index(string? searchTerm, int page = 1)
    {
        const int pageSize = 12;

        var query = _db.Suppliers
            .Where(s => s.IsApproved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(s =>
                s.Name.Contains(searchTerm) ||
                (s.City != null && s.City.Contains(searchTerm)) ||
                (s.Materials != null && s.Materials.Contains(searchTerm)));

        query = query.OrderBy(s => s.Name);

        var suppliers = await PaginatedList<Supplier>.CreateAsync(query, page, pageSize);

        ViewBag.SearchTerm = searchTerm;
        ViewBag.PageIndex  = suppliers.PageIndex;
        ViewBag.TotalPages = suppliers.TotalPages;
        ViewBag.TotalCount = suppliers.TotalCount;
        return View(suppliers.Items);
    }

    // GET: /Suppliers/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var supplier = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == id && s.IsApproved);
        if (supplier == null)
            return NotFound();

        return View(supplier);
    }

    // ══════════════════════════════════════════════════════
    // Admin — Create
    // ══════════════════════════════════════════════════════

    // GET: /Suppliers/Create
    [HttpGet]
    [Authorize(Roles = SeedData.AdminRole)]
    public IActionResult Create()
    {
        return View(new Supplier());
    }

    // POST: /Suppliers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> Create(Supplier model, IFormFile? logo)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" | ",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return View(model);
        }

        try
        {
            if (logo != null && logo.Length > 0)
                model.LogoPath = await _fileService.SaveImageAsync(logo, "suppliers");

            model.IsApproved = true;
            model.CreatedAt  = DateTime.UtcNow;

            _db.Suppliers.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = _T["تم إنشاء المورد «{0}» بنجاح.", model.Name].Value;
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(model);
        }
    }

    // ══════════════════════════════════════════════════════
    // Admin — Edit
    // ══════════════════════════════════════════════════════

    // GET: /Suppliers/Edit/5
    [HttpGet]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound();

        return View(supplier);
    }

    // POST: /Suppliers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> Edit(int id, Supplier model, IFormFile? logo)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound();

        supplier.Name        = model.Name;
        supplier.Description = model.Description;
        supplier.City        = model.City;
        supplier.Address     = model.Address;
        supplier.Phone       = model.Phone;
        supplier.Email       = model.Email;
        supplier.Website     = model.Website;
        supplier.Materials   = model.Materials;
        supplier.IsApproved  = model.IsApproved;

        if (!string.IsNullOrWhiteSpace(supplier.Name))
        {
            try
            {
                if (logo != null && logo.Length > 0)
                {
                    await _fileService.DeleteAsync(supplier.LogoPath);
                    supplier.LogoPath = await _fileService.SaveImageAsync(logo, "suppliers");
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = _T["تم تحديث بيانات المورد بنجاح."].Value;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
        }
        else
        {
            TempData["Error"] = _T["اسم المورد مطلوب."].Value;
        }

        return View(supplier);
    }

    // ══════════════════════════════════════════════════════
    // Admin — Delete
    // ══════════════════════════════════════════════════════

    // POST: /Suppliers/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null)
            return NotFound();

        await _fileService.DeleteAsync(supplier.LogoPath);
        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم حذف المورد «{0}» بنجاح.", supplier.Name].Value;
        return RedirectToAction(nameof(Index));
    }
}
