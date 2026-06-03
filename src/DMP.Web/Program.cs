using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Globalization;

// تحديد مسار wwwroot بشكل صريح لضمان عمل الملفات الثابتة من VS
var projectDir = Directory.GetCurrentDirectory();
var webRootPath = Path.Combine(projectDir, "wwwroot");
if (!Directory.Exists(webRootPath))
{
    var dir = new DirectoryInfo(projectDir);
    while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "wwwroot")))
        dir = dir.Parent;
    if (dir != null) webRootPath = Path.Combine(dir.FullName, "wwwroot");
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = webRootPath
});

// Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// DB
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(opt =>
{
    opt.Password.RequireDigit           = true;
    opt.Password.RequiredLength         = 6;
    opt.Password.RequireNonAlphanumeric = true;
    opt.Password.RequireUppercase       = true;
    opt.SignIn.RequireConfirmedAccount   = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(opt =>
{
    opt.LoginPath        = "/Account/Login";
    opt.LogoutPath       = "/Account/Logout";
    opt.AccessDeniedPath = "/Account/AccessDenied";
});

// Localization
builder.Services.AddLocalization(opt => opt.ResourcesPath = "");
builder.Services.Configure<RequestLocalizationOptions>(opt =>
{
    var cultures = new[] { new CultureInfo("ar"), new CultureInfo("en") };
    opt.DefaultRequestCulture   = new RequestCulture("ar");
    opt.SupportedCultures       = cultures;
    opt.SupportedUICultures     = cultures;
    opt.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider()
    };
});

builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(opt =>
    {
        opt.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(DMP.Web.SharedResource));
    });

builder.Services.AddScoped<IFileService, DMP.Web.Services.FileService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await SeedData.InitializeAsync(scope.ServiceProvider);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
