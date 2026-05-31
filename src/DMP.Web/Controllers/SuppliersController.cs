using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Helpers;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

public class SuppliersController : Controller
{
    private readonly ApplicationDbContext _db;

    public SuppliersController(ApplicationDbContext db)
    {
        _db = db;
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
}
