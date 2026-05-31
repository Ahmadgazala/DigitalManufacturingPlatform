using DMP.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace DMP.Web.Data;

public static class SeedData
{
    public const string AdminRole        = "Admin";
    public const string ManufacturerRole = "Manufacturer";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // إنشاء الأدوار
        foreach (var role in new[] { AdminRole, ManufacturerRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Admin seed
        const string adminEmail = "admin@dmp.jo";
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName  = adminEmail,
                Email     = adminEmail,
                FullName  = "مدير النظام",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, AdminRole);
        }

        // Manufacturer seed
        const string makerEmail = "workshop@dmp.jo";
        if (await userManager.FindByEmailAsync(makerEmail) is null)
        {
            var maker = new ApplicationUser
            {
                UserName  = makerEmail,
                Email     = makerEmail,
                FullName  = "ورشة المصنّع",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(maker, "Maker@123");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(maker, ManufacturerRole);
        }
    }
}
