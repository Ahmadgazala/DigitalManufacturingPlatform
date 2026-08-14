using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.ViewModels;

namespace DMP.Web.Controllers;

[Authorize(Roles = SeedData.ManufacturerRole)]
public class MachinesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;

    public MachinesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T)
    {
        _db = db;
        _userManager = userManager;
        _T = T;
    }

    private async Task<Manufacturer?> GetCurrentManufacturerAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        return await _db.Manufacturers.FirstOrDefaultAsync(m => m.UserId == userId);
    }

    // GET: /Machines  →  يُعيد توجيه للوحة المصنّع
    public IActionResult Index()
        => RedirectToAction("Dashboard", "Manufacturers");

    // GET: /Machines/Add
    [HttpGet]
    public async Task<IActionResult> Add()
    {
        var manufacturer = await GetCurrentManufacturerAsync();
        if (manufacturer == null)
        {
            TempData["Error"] = _T["يرجى إنشاء ملف الورشة أولاً."].Value;
            return RedirectToAction("Edit", "Manufacturers");
        }

        var vm = new MachineFormViewModel { ManufacturerId = manufacturer.Id };
        return View(vm);
    }

    // POST: /Machines/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(MachineFormViewModel vm)
    {
        var manufacturer = await GetCurrentManufacturerAsync();
        if (manufacturer == null)
            return Forbid();

        vm.ManufacturerId = manufacturer.Id;

        if (!ModelState.IsValid)
            return View(vm);

        _db.Machines.Add(new Machine
        {
            ManufacturerId = manufacturer.Id,
            Category = vm.Category,
            ModelName = vm.ModelName,
            Description = vm.Description,
            SupportedMaterials = vm.SupportedMaterials,
            PricingNote = vm.PricingNote
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = _T["تم إضافة الماكينة بنجاح."].Value;
        return RedirectToAction("Dashboard", "Manufacturers");
    }

    // GET: /Machines/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var manufacturer = await GetCurrentManufacturerAsync();
        if (manufacturer == null)
            return Forbid();

        var machine = await _db.Machines.FirstOrDefaultAsync(m => m.Id == id && m.ManufacturerId == manufacturer.Id);
        if (machine == null)
            return NotFound();

        var vm = new MachineFormViewModel
        {
            Id = machine.Id,
            ManufacturerId = machine.ManufacturerId,
            Category = machine.Category,
            ModelName = machine.ModelName,
            Description = machine.Description,
            SupportedMaterials = machine.SupportedMaterials,
            PricingNote = machine.PricingNote
        };

        return View(vm);
    }

    // POST: /Machines/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MachineFormViewModel vm)
    {
        var manufacturer = await GetCurrentManufacturerAsync();
        if (manufacturer == null)
            return Forbid();

        var machine = await _db.Machines.FirstOrDefaultAsync(m => m.Id == id && m.ManufacturerId == manufacturer.Id);
        if (machine == null)
            return NotFound();

        vm.ManufacturerId = manufacturer.Id;

        if (!ModelState.IsValid)
            return View(vm);

        machine.Category = vm.Category;
        machine.ModelName = vm.ModelName;
        machine.Description = vm.Description;
        machine.SupportedMaterials = vm.SupportedMaterials;
        machine.PricingNote = vm.PricingNote;

        await _db.SaveChangesAsync();
        TempData["Success"] = _T["تم تحديث بيانات الماكينة."].Value;
        return RedirectToAction("Dashboard", "Manufacturers");
    }

    // POST: /Machines/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var manufacturer = await GetCurrentManufacturerAsync();
        if (manufacturer == null)
            return Forbid();

        var machine = await _db.Machines.FirstOrDefaultAsync(m => m.Id == id && m.ManufacturerId == manufacturer.Id);
        if (machine == null)
            return NotFound();

        _db.Machines.Remove(machine);
        await _db.SaveChangesAsync();

        TempData["Success"] = _T["تم حذف الماكينة."].Value;
        return RedirectToAction("Dashboard", "Manufacturers");
    }
}
