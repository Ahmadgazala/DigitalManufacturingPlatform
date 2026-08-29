using DMP.Web.Data;
using DMP.Web.Models;
using DMP.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Globalization;

// إيجاد مجلد wwwroot الحقيقي بغض النظر عن مجلد التشغيل (VS أو terminal)
// AppContext.BaseDirectory = bin\Debug\net9.0\ → نصعد حتى نجد wwwroot
static string? FindWebRoot()
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, "wwwroot");
        if (Directory.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    return null;
}

var webRootPath = FindWebRoot();
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args        = args,
    WebRootPath = webRootPath
});

// Disable config file watchers (inotify) to avoid hitting Render's per-process limit
builder.Configuration.Sources.Clear();
var env = builder.Environment;
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

// Bind to Render's PORT env var (or fall back to appsettings / 5000)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// DB — PostgreSQL (Render) or SQLite (local dev)
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
var dbUrl   = Environment.GetEnvironmentVariable("DATABASE_URL");
var usePg   = dbUrl != null && dbUrl.StartsWith("postgresql", StringComparison.OrdinalIgnoreCase);
if (usePg)
{
    // Parse Render's postgresql:// URL into Npgsql connection string format
    var uri  = new Uri(dbUrl);
    var user = uri.UserInfo.Split(':')[0];
    var pass = Uri.UnescapeDataString(uri.UserInfo.Split(':')[1]);
    connStr  = $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={pass};SSL Mode=Prefer;Trust Server Certificate=true";
}

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
{
    if (usePg)
        opt.UseNpgsql(connStr);
    else
        opt.UseSqlite(connStr);
});

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

// في بيئة التطوير: تحقق من صلاحية الجلسة كل دقيقتين بدلاً من 30 دقيقة
// يمنع خطأ FK عند إعادة تعيين قاعدة البيانات مع بقاء الكوكي القديم
builder.Services.Configure<SecurityStampValidatorOptions>(opt =>
    opt.ValidationInterval = TimeSpan.FromMinutes(2));

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

_ = Task.Run(async () =>
{
    for (int attempt = 1; attempt <= 10; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();

            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ""Products"" (
                    ""Id"" SERIAL PRIMARY KEY,
                    ""Name"" VARCHAR(200) NOT NULL,
                    ""Description"" VARCHAR(2000),
                    ""Price"" NUMERIC(18,2) NOT NULL,
                    ""ImagePath"" VARCHAR(200),
                    ""Category"" INTEGER NOT NULL,
                    ""SellerType"" INTEGER NOT NULL,
                    ""SellerUserId"" TEXT,
                    ""ManufacturerId"" INTEGER,
                    ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE,
                    ""Stock"" INTEGER NOT NULL DEFAULT 0,
                    ""CreatedAt"" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    CONSTRAINT ""FK_Products_AspNetUsers_SellerUserId"" FOREIGN KEY (""SellerUserId"") REFERENCES ""AspNetUsers""(""Id""),
                    CONSTRAINT ""FK_Products_Manufacturers_ManufacturerId"" FOREIGN KEY (""ManufacturerId"") REFERENCES ""Manufacturers""(""Id"")
                );
                CREATE INDEX IF NOT EXISTS ""IX_Products_SellerUserId"" ON ""Products"" (""SellerUserId"");
                CREATE INDEX IF NOT EXISTS ""IX_Products_ManufacturerId"" ON ""Products"" (""ManufacturerId"");

                DELETE FROM ""Products"" WHERE ""Name"" = 'test number 1' OR ""Name"" = 'filment';
            ";
            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();

            await SeedData.InitializeAsync(scope.ServiceProvider);
            return;
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "DB init attempt {Attempt} failed, retrying in {Delay}s...", attempt, attempt * 3);
            await Task.Delay(attempt * 3000);
        }
    }
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// خدمة الملفات الثابتة مباشرة من المجلد الحقيقي (يتجاوز StaticWebAssets الذي يسبب 404 في VS)
if (webRootPath != null)
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(webRootPath),
        RequestPath  = ""
    });
else
    app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("healthy"));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
