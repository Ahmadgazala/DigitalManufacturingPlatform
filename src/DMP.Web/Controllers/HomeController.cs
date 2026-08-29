using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DMP.Web.Data;
using DMP.Web.Models;

namespace DMP.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var latestManufacturers = await _db.Manufacturers
                .Where(m => m.IsApproved)
                .OrderByDescending(m => m.CreatedAt)
                .Take(6)
                .ToListAsync();

            var activeCampaigns = await _db.GroupBuyingCampaigns
                .Where(c => c.Status == CampaignStatus.Active && c.DeadlineDate > DateTime.UtcNow)
                .OrderByDescending(c => c.CreatedAt)
                .Take(3)
                .ToListAsync();

            ViewBag.LatestManufacturers = latestManufacturers;
            ViewBag.ActiveCampaigns = activeCampaigns;

            ViewBag.ApprovedWorkshops = await _db.Manufacturers.CountAsync(m => m.IsApproved);
            ViewBag.CityCount = await _db.Manufacturers
                .Where(m => m.IsApproved && m.City != null && m.City != "")
                .Select(m => m.City)
                .Distinct()
                .CountAsync();
            ViewBag.CompletedRequests = await _db.ManufacturingRequests
                .CountAsync(r => r.Status == RequestStatus.Completed);
        }
        catch
        {
            ViewBag.LatestManufacturers = new List<Manufacturer>();
            ViewBag.ActiveCampaigns = new List<GroupBuyingCampaign>();
            ViewBag.ApprovedWorkshops = 0;
            ViewBag.CityCount = 0;
            ViewBag.CompletedRequests = 0;
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
