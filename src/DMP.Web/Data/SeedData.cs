using DMP.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DMP.Web.Data;

public static class SeedData
{
    public const string AdminRole        = "Admin";
    public const string ManufacturerRole = "Manufacturer";
    public const string CustomerRole     = "Customer";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db          = services.GetRequiredService<ApplicationDbContext>();

        // ── Roles ────────────────────────────────────────────────
        foreach (var role in new[] { AdminRole, ManufacturerRole, CustomerRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // ── Admin ────────────────────────────────────────────────
        await EnsureUser(userManager, new ApplicationUser
        {
            UserName = "admin@dmp.jo", Email = "admin@dmp.jo",
            FullName = "مدير النظام", EmailConfirmed = true
        }, "Admin@123", AdminRole);

        // ── Customers ────────────────────────────────────────────
        var customers = new[]
        {
            ("ali@dmp.jo",    "علي الأحمد",    "Customer@123"),
            ("sara@dmp.jo",   "سارة المنصور",  "Customer@123"),
            ("omar@dmp.jo",   "عمر الخالدي",   "Customer@123"),
            ("reem@dmp.jo",   "ريم العمري",    "Customer@123"),
            ("customer@dmp.jo","عميل تجريبي",  "Customer@123"),
        };

        foreach (var (email, name, pass) in customers)
        {
            await EnsureUser(userManager, new ApplicationUser
            {
                UserName = email, Email = email,
                FullName = name, EmailConfirmed = true
            }, pass, CustomerRole);
        }

        // ── Manufacturers ─────────────────────────────────────────
        var makers = new[]
        {
            ("workshop@dmp.jo",  "ورشة المصنّع",      "Maker@123"),
            ("maker1@dmp.jo",    "ورشة الإبداع",      "Maker@123"),
            ("maker2@dmp.jo",    "تك فاب",             "Maker@123"),
            ("maker3@dmp.jo",    "ورشة الخليج",        "Maker@123"),
            ("maker4@dmp.jo",    "الريادة للتصنيع",    "Maker@123"),
        };

        foreach (var (email, name, pass) in makers)
        {
            await EnsureUser(userManager, new ApplicationUser
            {
                UserName = email, Email = email,
                FullName = name, EmailConfirmed = true
            }, pass, ManufacturerRole);
        }

        // ── Manufacturer Profiles ─────────────────────────────────
        await EnsureManufacturerProfiles(db, userManager);

        // ── Group Buying Campaigns ────────────────────────────────
        await EnsureCampaigns(db, userManager);
    }

    // ─────────────────────────────────────────────────────────────
    private static async Task EnsureCampaigns(
        ApplicationDbContext db,
        UserManager<ApplicationUser> um)
    {
        if (await db.GroupBuyingCampaigns.AnyAsync()) return;

        // المصنّعين الذين سينشئون الحملات
        var maker1 = await um.FindByEmailAsync("maker1@dmp.jo");
        var maker2 = await um.FindByEmailAsync("maker2@dmp.jo");
        var maker4 = await um.FindByEmailAsync("maker4@dmp.jo");
        var workshop = await um.FindByEmailAsync("workshop@dmp.jo");

        var campaigns = new List<GroupBuyingCampaign>
        {
            new()
            {
                Title           = "طباعة ثلاثية الأبعاد — نماذج هندسية",
                Description     = "حملة شراء جماعي لطباعة نماذج هندسية بمادة PLA عالية الجودة. مناسبة للمهندسين والطلاب. الطلب الأدنى 50 قطعة للحصول على سعر الجملة.",
                ItemName        = "نموذج هندسي PLA",
                IndividualPrice = 15,
                GroupPrice      = 9,
                MinQuantity     = 50,
                CurrentQuantity = 31,
                Status          = CampaignStatus.Active,
                DeadlineDate    = DateTime.UtcNow.AddDays(18)
            },
            new()
            {
                Title           = "قطع ليزر — لافتات أكريليك مضيئة",
                Description     = "لافتات أكريليك مضيئة بقطع ليزر عالي الدقة. مثالية للمحلات التجارية والمكاتب. التخصيص متاح على الاسم والشعار.",
                ItemName        = "لافتة أكريليك مضيئة 40×30 سم",
                IndividualPrice = 45,
                GroupPrice      = 28,
                MinQuantity     = 30,
                CurrentQuantity = 30,
                Status          = CampaignStatus.AwaitingPayment,
                DeadlineDate    = DateTime.UtcNow.AddDays(3)
            },
            new()
            {
                Title           = "تشغيل CNC — قطع ألومنيوم للصناعة",
                Description     = "قطع ألومنيوم مشغولة بـ CNC للاستخدام الصناعي والميكانيكي. دقة عالية حتى 0.05mm. مناسبة لشركات التصنيع والمصانع الصغيرة.",
                ItemName        = "قطعة ألومنيوم CNC مخصصة",
                IndividualPrice = 80,
                GroupPrice      = 52,
                MinQuantity     = 20,
                CurrentQuantity = 8,
                Status          = CampaignStatus.Active,
                DeadlineDate    = DateTime.UtcNow.AddDays(25)
            },
            new()
            {
                Title           = "لوحات PCB — بروتوتايب للمطورين",
                Description     = "تصنيع لوحات PCB مخصصة للمطورين وشركات الإلكترونيات. طبقتان، مواصفات احترافية. مناسبة للستارتاب وعشاق الإلكترونيات.",
                ItemName        = "لوحة PCB مخصصة 10×10 سم",
                IndividualPrice = 35,
                GroupPrice      = 20,
                MinQuantity     = 40,
                CurrentQuantity = 12,
                Status          = CampaignStatus.Active,
                DeadlineDate    = DateTime.UtcNow.AddDays(30)
            },
            new()
            {
                Title           = "أثاث خشبي CNC — مكاتب هوم أوفيس",
                Description     = "مكاتب هوم أوفيس مصنوعة من الخشب الطبيعي بتشغيل CNC. تصاميم عصرية قابلة للتخصيص. سعر الجملة يوفر 40% من السعر الفردي.",
                ItemName        = "مكتب هوم أوفيس خشبي",
                IndividualPrice = 250,
                GroupPrice      = 150,
                MinQuantity     = 10,
                CurrentQuantity = 6,
                Status          = CampaignStatus.Active,
                DeadlineDate    = DateTime.UtcNow.AddDays(45)
            },
            new()
            {
                Title           = "حملة قطع ليزر أكريليك ملون",
                Description     = "حملة منتهية لقطع الأكريليك الملون. تم التسليم بنجاح لجميع المشتركين.",
                ItemName        = "قطعة أكريليك ملونة",
                IndividualPrice = 20,
                GroupPrice      = 12,
                MinQuantity     = 25,
                CurrentQuantity = 25,
                Status          = CampaignStatus.Delivered,
                DeadlineDate    = DateTime.UtcNow.AddDays(-10)
            },
        };

        db.GroupBuyingCampaigns.AddRange(campaigns);
        await db.SaveChangesAsync();

        // أضف بعض المشاركين للحملات النشطة
        var customers = new[]
        {
            await um.FindByEmailAsync("ali@dmp.jo"),
            await um.FindByEmailAsync("sara@dmp.jo"),
            await um.FindByEmailAsync("omar@dmp.jo"),
            await um.FindByEmailAsync("reem@dmp.jo"),
        };

        // حملة 1 — 31 مشارك موزعين
        var c1 = campaigns[0];
        var participantData = new[] { (customers[0], 8), (customers[1], 10), (customers[2], 7), (customers[3], 6) };
        foreach (var (user, qty) in participantData)
        {
            if (user == null) continue;
            db.CampaignParticipants.Add(new CampaignParticipant
            {
                CampaignId = c1.Id, UserId = user.Id,
                Quantity = qty, JoinedAt = DateTime.UtcNow.AddDays(-5)
            });
        }

        // حملة 3 — 8 مشاركين
        var c3 = campaigns[2];
        foreach (var (user, qty) in new[] { (customers[0], 3), (customers[1], 5) })
        {
            if (user == null) continue;
            db.CampaignParticipants.Add(new CampaignParticipant
            {
                CampaignId = c3.Id, UserId = user.Id,
                Quantity = qty, JoinedAt = DateTime.UtcNow.AddDays(-3)
            });
        }

        await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────
    private static async Task EnsureUser(
        UserManager<ApplicationUser> um,
        ApplicationUser user, string password, string role)
    {
        if (await um.FindByEmailAsync(user.Email!) is not null) return;

        var result = await um.CreateAsync(user, password);
        if (result.Succeeded)
            await um.AddToRoleAsync(user, role);
    }

    // ─────────────────────────────────────────────────────────────
    private static async Task EnsureManufacturerProfiles(
        ApplicationDbContext db,
        UserManager<ApplicationUser> um)
    {
        var profiles = new[]
        {
            new
            {
                Email      = "workshop@dmp.jo",
                Workshop   = "ورشة المصنّع",
                City       = "عمّان",
                Bio        = "ورشة متخصصة في الطباعة ثلاثية الأبعاد وقطع الليزر. نخدم عملاء الأردن منذ 2019.",
                Phone      = "0791234567",
                Delivery   = DeliveryOption.LocalDelivery,
                IsApproved = true,
                Lat        = 31.9539,
                Lng        = 35.9106,
                Category   = MachineCategory.Printing3D,
                Machine    = "Creality Ender 5 Pro",
                MachineDesc= "طابعة ثلاثية الأبعاد احترافية — دقة 0.1mm"
            },
            new
            {
                Email      = "maker1@dmp.jo",
                Workshop   = "ورشة الإبداع",
                City       = "إربد",
                Bio        = "متخصصون في قطع الليزر والأكريليك. نوفر خدمة التوصيل لجميع المحافظات.",
                Phone      = "0782345678",
                Delivery   = DeliveryOption.Nationwide,
                IsApproved = true,
                Lat        = 32.5556,
                Lng        = 35.8500,
                Category   = MachineCategory.LaserCutting,
                Machine    = "xTool D1 Pro",
                MachineDesc= "ليزر 20W عالي الدقة للقطع والنقش"
            },
            new
            {
                Email      = "maker2@dmp.jo",
                Workshop   = "تك فاب",
                City       = "الزرقاء",
                Bio        = "نقدم خدمات CNC وتشغيل المعادن للقطاع الصناعي والأفراد.",
                Phone      = "0773456789",
                Delivery   = DeliveryOption.LocalDelivery,
                IsApproved = true,
                Lat        = 32.0728,
                Lng        = 36.0875,
                Category   = MachineCategory.CNC,
                Machine    = "Haas Mini Mill",
                MachineDesc= "مركز تشغيل CNC للمعادن والبلاستيك"
            },
            new
            {
                Email      = "maker3@dmp.jo",
                Workshop   = "ورشة الخليج",
                City       = "العقبة",
                Bio        = "ورشة نجارة متقدمة مع CNC للخشب. نصنع الأثاث والديكور حسب الطلب.",
                Phone      = "0764567890",
                Delivery   = DeliveryOption.PickupOnly,
                IsApproved = false,
                Lat        = 29.5266,
                Lng        = 35.0078,
                Category   = MachineCategory.Woodwork,
                Machine    = "ShopBot PRSalpha",
                MachineDesc= "راوتر CNC للخشب — مساحة عمل 2.4×1.2م"
            },
            new
            {
                Email      = "maker4@dmp.jo",
                Workshop   = "الريادة للتصنيع",
                City       = "عمّان",
                Bio        = "تصنيع لوحات PCB ونماذج إلكترونية. نخدم شركات الستارتاب والمطورين.",
                Phone      = "0795678901",
                Delivery   = DeliveryOption.Nationwide,
                IsApproved = true,
                Lat        = 31.9800,
                Lng        = 35.9350,
                Category   = MachineCategory.PCB,
                Machine    = "LPKF ProtoMat S104",
                MachineDesc= "ماكينة تصنيع PCB احترافية — طبقتين"
            },
        };

        foreach (var p in profiles)
        {
            var user = await um.FindByEmailAsync(p.Email);
            if (user == null) continue;

            // تحقق إذا يوجد ملف مسبقاً
            if (await db.Manufacturers.AnyAsync(m => m.UserId == user.Id)) continue;

            var mfr = new Manufacturer
            {
                UserId            = user.Id,
                WorkshopName      = p.Workshop,
                City              = p.City,
                Bio               = p.Bio,
                ContactPhone      = p.Phone,
                ContactEmail      = p.Email,
                Delivery          = p.Delivery,
                IsApproved        = p.IsApproved,
                Latitude          = p.Lat,
                Longitude         = p.Lng,
                WorkingHours      = "السبت – الخميس: 9ص – 6م",
                ProductionCapacity= "حتى 100 قطعة / أسبوع"
            };

            db.Manufacturers.Add(mfr);
            await db.SaveChangesAsync();

            // أضف ماكينة
            db.Machines.Add(new Machine
            {
                ManufacturerId    = mfr.Id,
                Category          = p.Category,
                ModelName         = p.Machine,
                Description       = p.MachineDesc,
                SupportedMaterials= "PLA, ABS, Wood, Acrylic, Metal",
                PricingNote       = "تواصل للحصول على عرض سعر"
            });
            await db.SaveChangesAsync();
        }
    }
}
