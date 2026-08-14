using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.ViewModels;

namespace DMP.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _T = T;
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, _T["البريد الإلكتروني أو كلمة المرور غير صحيحة."].Value);
        return View(model);
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // تحديد الدور: عميل أو مصنّع فقط (منع تعيين Admin من التسجيل)
            var role = model.Role == SeedData.ManufacturerRole
                ? SeedData.ManufacturerRole
                : SeedData.CustomerRole;

            await _userManager.AddToRoleAsync(user, role);
            await _signInManager.SignInAsync(user, isPersistent: false);

            if (role == SeedData.ManufacturerRole)
            {
                TempData["Success"] = _T["تم إنشاء حسابك بنجاح. يمكنك الآن إكمال ملف الورشة."].Value;
                return RedirectToAction("Edit", "Manufacturers");
            }
            else
            {
                TempData["Success"] = _T["مرحباً! تم إنشاء حسابك بنجاح."].Value;
                return RedirectToAction("Index", "Home");
            }
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    // POST: /Account/Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/AccessDenied
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
