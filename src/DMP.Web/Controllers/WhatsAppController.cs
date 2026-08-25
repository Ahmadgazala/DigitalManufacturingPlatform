using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DMP.Web.Services;

namespace DMP.Web.Controllers;

[Authorize]
public class WhatsAppController : Controller
{
    private readonly IExcelService _excelService;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(
        IExcelService excelService,
        IWhatsAppService whatsAppService,
        ILogger<WhatsAppController> logger)
    {
        _excelService = excelService;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    // GET: /WhatsApp
    public IActionResult Index()
    {
        return View();
    }

    // POST: /WhatsApp/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upload(IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["Error"] = "Please upload an Excel file";
            return RedirectToAction(nameof(Index));
        }

        var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
        {
            TempData["Error"] = "Only Excel files (.xlsx, .xls) are allowed";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var phoneNumbers = _excelService.ReadPhoneNumbers(excelFile);

            if (phoneNumbers.Count == 0)
            {
                TempData["Error"] = "No phone numbers found in the Excel file";
                return RedirectToAction(nameof(Index));
            }

            // Store in TempData for preview
            TempData["PhoneNumbers"] = System.Text.Json.JsonSerializer.Serialize(phoneNumbers);
            TempData["PhoneCount"] = phoneNumbers.Count;
            TempData["FileName"] = excelFile.FileName;

            return RedirectToAction(nameof(Preview));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Excel file");
            TempData["Error"] = $"Error reading file: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /WhatsApp/Preview
    public IActionResult Preview()
    {
        var phoneNumbersJson = TempData["PhoneNumbers"] as string;
        if (string.IsNullOrEmpty(phoneNumbersJson))
        {
            TempData["Error"] = "No phone numbers to preview. Please upload a file first.";
            return RedirectToAction(nameof(Index));
        }

        var phoneNumbers = System.Text.Json.JsonSerializer.Deserialize<List<string>>(phoneNumbersJson);
        ViewBag.PhoneNumbers = phoneNumbers;
        ViewBag.PhoneCount = TempData["PhoneCount"];
        ViewBag.FileName = TempData["FileName"];

        // Keep data for the create action
        TempData.Keep("PhoneNumbers");

        return View();
    }

    // POST: /WhatsApp/CreateGroup
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(string groupName)
    {
        var phoneNumbersJson = TempData["PhoneNumbers"] as string;
        if (string.IsNullOrEmpty(phoneNumbersJson))
        {
            TempData["Error"] = "No phone numbers available. Please upload a file first.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(groupName))
        {
            TempData["Error"] = "Please enter a group name";
            return RedirectToAction(nameof(Preview));
        }

        var phoneNumbers = System.Text.Json.JsonSerializer.Deserialize<List<string>>(phoneNumbersJson);

        try
        {
            var result = await _whatsAppService.CreateGroupAsync(groupName, phoneNumbers!);

            if (result.Success)
            {
                TempData["Success"] = $"Group '{groupName}' created successfully with {result.ParticipantsAdded} participants";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["Error"] = $"Failed to create group: {result.ErrorMessage}";
                return RedirectToAction(nameof(Preview));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating WhatsApp group");
            TempData["Error"] = $"Error creating group: {ex.Message}";
            return RedirectToAction(nameof(Preview));
        }
    }

    // GET: /WhatsApp/Groups
    public async Task<IActionResult> Groups()
    {
        var groups = await _whatsAppService.GetGroupsAsync();
        return View(groups);
    }
}
