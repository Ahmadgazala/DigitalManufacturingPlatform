using DMP.Web.Data;
using DMP.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DMP.Web.Services;

public class CartService
{
    private const string CookieName = "jomaker_cart";
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public CartService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    /// <summary>قراءة معرف السلة من كوكي، وإنشاؤه إذا لم يوجد.</summary>
    public string GetCartKey()
    {
        var ctx = _http.HttpContext!;
        var key = ctx.Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(key))
        {
            key = Guid.NewGuid().ToString("N");
            ctx.Response.Cookies.Append(CookieName, key, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                MaxAge   = TimeSpan.FromDays(30)
            });
        }
        return key;
    }

    public async Task<List<CartItem>> GetItemsAsync()
    {
        var key = GetCartKey();
        return await _db.CartItems
            .Include(c => c.Product).ThenInclude(p => p.Images)
            .Where(c => c.CartKey == key)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCountAsync()
    {
        var key = GetCartKey();
        return await _db.CartItems
            .Where(c => c.CartKey == key)
            .SumAsync(c => (int?)c.Quantity) ?? 0;
    }

    public async Task<decimal> GetTotalAsync()
    {
        var items = await GetItemsAsync();
        return items.Sum(c => (c.Product?.Price ?? 0) * c.Quantity);
    }

    public async Task AddAsync(int productId, int quantity = 1)
    {
        var key = GetCartKey();
        var existing = await _db.CartItems
            .FirstOrDefaultAsync(c => c.CartKey == key && c.ProductId == productId);

        if (existing != null)
            existing.Quantity += quantity;
        else
            _db.CartItems.Add(new CartItem
            {
                CartKey   = key,
                ProductId = productId,
                Quantity  = quantity
            });

        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(int productId, int quantity)
    {
        var key = GetCartKey();
        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.CartKey == key && c.ProductId == productId);

        if (item == null) return;

        if (quantity <= 0)
            _db.CartItems.Remove(item);
        else
            item.Quantity = quantity;

        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(int productId)
    {
        var key = GetCartKey();
        var item = await _db.CartItems
            .FirstOrDefaultAsync(c => c.CartKey == key && c.ProductId == productId);

        if (item != null)
        {
            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    public async Task ClearAsync()
    {
        var key = GetCartKey();
        var items = await _db.CartItems
            .Where(c => c.CartKey == key)
            .ToListAsync();
        if (items.Count > 0)
        {
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();
        }
    }
}