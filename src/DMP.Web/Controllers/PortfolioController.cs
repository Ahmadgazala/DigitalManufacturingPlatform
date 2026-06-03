using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.Services;

namespace DMP.Web.Controllers;

[Authorize(Roles = "Manufacturer")]
public class PortfolioController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileService _fileService;

    public PortfolioController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileService fileService)
    {
        _db = db;
        _userManager = userManager;
        _fileService = fileService;
    }

    private async Task<Manufacturer?> GetMyManufacturer()
    {
        var userId = _userManager.GetUserId(User)!;
        return await _db.Manufacturers.FirstOrDefaultAsync(m => m.UserId == userId);
    }

    // GET: /Portfolio/Create
    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Categories = Enum.GetValues<MachineCategory>();
        return View();
    }

    // POST: /Portfolio/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PortfolioItem model, IFormFile? image)
    {
        ViewBag.Categories = Enum.GetValues<MachineCategory>();
        var mfr = await GetMyManufacturer();
        if (mfr == null) return RedirectToAction("Edit", "Manufacturers");

        if (!ModelState.IsValid) return View(model);

        model.ManufacturerId = mfr.Id;

        if (image != null)
        {
            var path = await _fileService.SaveFileAsync(image, "portfolio");
            if (path != null) model.ImagePath = path;
        }

        _db.PortfolioItems.Add(model);
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم إضافة المشروع للملف الشخصي.";
        return RedirectToAction("Dashboard", "Manufacturers");
    }

    // GET: /Portfolio/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var mfr = await GetMyManufacturer();
        if (mfr == null) return NotFound();

        var item = await _db.PortfolioItems
            .FirstOrDefaultAsync(p => p.Id == id && p.ManufacturerId == mfr.Id);
        if (item == null) return NotFound();

        ViewBag.Categories = Enum.GetValues<MachineCategory>();
        return View(item);
    }

    // POST: /Portfolio/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PortfolioItem model, IFormFile? image)
    {
        ViewBag.Categories = Enum.GetValues<MachineCategory>();
        var mfr = await GetMyManufacturer();
        if (mfr == null) return NotFound();

        var item = await _db.PortfolioItems
            .FirstOrDefaultAsync(p => p.Id == id && p.ManufacturerId == mfr.Id);
        if (item == null) return NotFound();

        if (!ModelState.IsValid) return View(model);

        item.Title       = model.Title;
        item.Description = model.Description;
        item.Category    = model.Category;
        item.CompletedAt = model.CompletedAt;

        if (image != null)
        {
            var path = await _fileService.SaveFileAsync(image, "portfolio");
            if (path != null) item.ImagePath = path;
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث المشروع.";
        return RedirectToAction("Dashboard", "Manufacturers");
    }

    // POST: /Portfolio/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var mfr = await GetMyManufacturer();
        if (mfr == null) return NotFound();

        var item = await _db.PortfolioItems
            .FirstOrDefaultAsync(p => p.Id == id && p.ManufacturerId == mfr.Id);
        if (item == null) return NotFound();

        _db.PortfolioItems.Remove(item);
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم حذف المشروع.";
        return RedirectToAction("Dashboard", "Manufacturers");
    }
}
