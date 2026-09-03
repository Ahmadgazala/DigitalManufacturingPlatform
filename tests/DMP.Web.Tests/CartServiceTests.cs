using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DMP.Web.Tests;

public class CartServiceTests
{
    private const string CookieName = "jomaker_cart";

    private sealed class Harness : IDisposable
    {
        public ApplicationDbContext Db { get; }
        public DefaultHttpContext Ctx { get; } = new();
        public CartService Svc { get; }

        public Harness()
        {
            Db = TestDb.Create();
            Svc = new CartService(Db, new TestHttpContextAccessor(Ctx));
        }

        public void SeedCartKey(string key)
            => Ctx.Request.Headers.Cookie = $"{CookieName}={key}";

        public string? CreatedCartCookie
        {
            get
            {
                var setCookie = Ctx.Response.Headers.SetCookie.ToString();
                return setCookie.Contains(CookieName + "=") ? setCookie : null;
            }
        }

        public void Dispose() => Db.Dispose();
    }

    private sealed class TestHttpContextAccessor : IHttpContextAccessor
    {
        public TestHttpContextAccessor(HttpContext ctx) => HttpContext = ctx;
        public HttpContext? HttpContext { get; set; }
    }

    private static Product MakeProduct(decimal price, string name = "Item")
        => new() { Name = name, Price = price, Stock = 10, IsActive = true };

    [Fact]
    public void GetCartKey_creates_new_key_when_none_present()
    {
        using var h = new Harness();

        var key = h.Svc.GetCartKey();

        Assert.False(string.IsNullOrEmpty(key));
        Assert.NotNull(h.CreatedCartCookie); // set-cookie written by service
        Assert.Contains(CookieName, h.CreatedCartCookie);
    }

    [Fact]
    public async Task AddAsync_increments_quantity_for_existing_product()
    {
        using var h = new Harness();
        h.SeedCartKey("cart-abc");
        var product = MakeProduct(10m);
        h.Db.Products.Add(product);
        await h.Db.SaveChangesAsync();

        await h.Svc.AddAsync(product.Id, 2);
        await h.Svc.AddAsync(product.Id, 3);

        var items = await h.Db.CartItems.ToListAsync();
        Assert.Single(items);
        Assert.Equal(5, items[0].Quantity);
    }

    [Fact]
    public async Task AddAsync_creates_separate_items_for_different_products()
    {
        using var h = new Harness();
        h.SeedCartKey("cart-abc");
        var p1 = MakeProduct(10m, "A");
        var p2 = MakeProduct(20m, "B");
        h.Db.Products.AddRange(p1, p2);
        await h.Db.SaveChangesAsync();

        await h.Svc.AddAsync(p1.Id, 1);
        await h.Svc.AddAsync(p2.Id, 2);

        Assert.Equal(2, await h.Db.CartItems.CountAsync());
    }

    [Fact]
    public async Task GetCountAsync_sums_quantities()
    {
        using var h = new Harness();
        h.SeedCartKey("cart-abc");
        var product = MakeProduct(10m);
        h.Db.Products.Add(product);
        await h.Db.SaveChangesAsync();

        await h.Svc.AddAsync(product.Id, 4);
        await h.Svc.AddAsync(product.Id, 3);

        Assert.Equal(7, await h.Svc.GetCountAsync());
    }

    [Fact]
    public async Task GetTotalAsync_multiplies_price_by_quantity()
    {
        using var h = new Harness();
        h.SeedCartKey("cart-abc");
        var p1 = MakeProduct(10m, "A");
        var p2 = MakeProduct(20.5m, "B");
        h.Db.Products.AddRange(p1, p2);
        await h.Db.SaveChangesAsync();

        await h.Svc.AddAsync(p1.Id, 2);  // 20
        await h.Svc.AddAsync(p2.Id, 1);  // 20.5

        Assert.Equal(40.5m, await h.Svc.GetTotalAsync());
    }

    [Fact]
    public async Task UpdateAsync_to_zero_removes_item()
    {
        using var h = new Harness();
        h.SeedCartKey("cart-abc");
        var product = MakeProduct(10m);
        h.Db.Products.Add(product);
        await h.Db.SaveChangesAsync();

        await h.Svc.AddAsync(product.Id, 1);
        await h.Svc.UpdateAsync(product.Id, 0);

        Assert.Empty(await h.Db.CartItems.ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_changes_quantity()
    {
        using var h = new Harness();
        h.SeedCartKey("cart-abc");
        var product = MakeProduct(10m);
        h.Db.Products.Add(product);
        await h.Db.SaveChangesAsync();

        await h.Svc.AddAsync(product.Id, 1);
        await h.Svc.UpdateAsync(product.Id, 9);

        var item = Assert.Single(await h.Db.CartItems.ToListAsync());
        Assert.Equal(9, item.Quantity);
    }

    [Fact]
    public async Task RemoveAsync_deletes_only_target_item()
    {
        using var h = new Harness();
        h.SeedCartKey("cart-abc");
        var p1 = MakeProduct(10m, "A");
        var p2 = MakeProduct(20m, "B");
        h.Db.Products.AddRange(p1, p2);
        await h.Db.SaveChangesAsync();

        await h.Svc.AddAsync(p1.Id, 1);
        await h.Svc.AddAsync(p2.Id, 1);
        await h.Svc.RemoveAsync(p1.Id);

        var remaining = await h.Db.CartItems.ToListAsync();
        var item = Assert.Single(remaining);
        Assert.Equal(p2.Id, item.ProductId);
    }

    [Fact]
    public async Task ClearAsync_removes_all_items_for_this_cart()
    {
        using var h = new Harness();
        h.SeedCartKey("cart-abc");
        var p1 = MakeProduct(10m, "A");
        var p2 = MakeProduct(20m, "B");
        h.Db.Products.AddRange(p1, p2);
        await h.Db.SaveChangesAsync();

        await h.Svc.AddAsync(p1.Id, 1);
        await h.Svc.AddAsync(p2.Id, 1);
        await h.Svc.ClearAsync();

        Assert.Empty(await h.Db.CartItems.ToListAsync());
    }
}
