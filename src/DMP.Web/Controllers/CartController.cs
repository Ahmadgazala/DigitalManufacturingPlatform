using DMP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using Microsoft.EntityFrameworkCore;

namespace DMP.Web.Controllers;

public class CartController : Controller
{
    private readonly CartService _cart;
    private readonly Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> _T;

    public CartController(
        CartService cart,
        Microsoft.Extensions.Localization.IStringLocalizer<DMP.Web.SharedResource> T)
    {
        _cart = cart;
        _T = T;
    }

    // GET: /Cart
    public async Task<IActionResult> Index()
    {
        var items = await _cart.GetItemsAsync();
        var outOfStock = items.Where(c => c.Product == null
                                       || !c.Product.IsActive
                                       || c.Product.Stock <= 0).ToList();
        ViewBag.OutOfStock = outOfStock;
        ViewBag.Total = await _cart.GetTotalAsync();
        return View(items);
    }

    // POST: /Cart/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        if (quantity < 1) quantity = 1;
        await _cart.AddAsync(productId, quantity);
        TempData["Success"] = _T["تمت إضافة المنتج إلى السلة."].Value;
        return RedirectToAction(nameof(Index));
    }

    // POST: /Cart/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int productId, int quantity)
    {
        await _cart.UpdateAsync(productId, quantity);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Cart/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int productId)
    {
        await _cart.RemoveAsync(productId);
        TempData["Success"] = _T["تمت إزالة المنتج من السلة."].Value;
        return RedirectToAction(nameof(Index));
    }

    // POST: /Cart/Clear
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        await _cart.ClearAsync();
        return RedirectToAction(nameof(Index));
    }
}