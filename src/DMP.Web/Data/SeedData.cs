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
            ("ali@dmp.jo",     "علي الأحمد",     "Customer@123"),
            ("sara@dmp.jo",    "سارة المنصور",   "Customer@123"),
            ("omar@dmp.jo",    "عمر الخالدي",    "Customer@123"),
            ("reem@dmp.jo",    "ريم العمري",     "Customer@123"),
            ("customer@dmp.jo","عميل تجريبي",    "Customer@123"),
            ("khaled@dmp.jo",  "خالد الزعبي",    "Customer@123"),
            ("nour@dmp.jo",    "نور الشريف",     "Customer@123"),
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
            ("workshop@dmp.jo",  "محمد القضاة",      "Maker@123"),
            ("maker1@dmp.jo",    "أحمد الزيادات",    "Maker@123"),
            ("maker2@dmp.jo",    "كريم نصرالله",     "Maker@123"),
            ("maker3@dmp.jo",    "سامي العبادي",     "Maker@123"),
            ("maker4@dmp.jo",    "يوسف الحموري",     "Maker@123"),
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

        // ── Reviews ───────────────────────────────────────────────
        await EnsureReviews(db, userManager);

        // ── Portfolio & Gallery ───────────────────────────────────
        await EnsurePortfolioAndGallery(db, userManager);

        // ── Manufacturing Requests ────────────────────────────────
        await EnsureRequests(db, userManager);

        // ── Group Buying Campaigns ────────────────────────────────
        await EnsureCampaigns(db, userManager);

        // ── Suppliers ─────────────────────────────────────────────
        await EnsureSuppliers(db);
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
                Address    = "منطقة ماركا الصناعية، شارع المصانع، مبنى 12",
                Bio        = "ورشة رائدة في الطباعة ثلاثية الأبعاد وقطع الليزر في عمّان. نمتلك أحدث المعدات ونخدم عملاءنا منذ 2019 بجودة عالية وأسعار منافسة. تخصصنا في النماذج الهندسية والمجسمات المعمارية والقطع الصناعية البديلة.",
                Phone      = "0791234567",
                Delivery   = DeliveryOption.LocalDelivery,
                IsApproved = true,
                Lat        = 31.9539,
                Lng        = 35.9106,
                Category   = MachineCategory.Printing3D,
                Machine    = "Creality Ender 5 Pro",
                MachineDesc= "طابعة ثلاثية الأبعاد احترافية — دقة 0.1mm، حجم طباعة 220×220×300mm",
                Materials  = "PLA, ABS, PETG, TPU, Resin",
                Profile    = "/uploads/seed/profile-workshop.jpg",
                Cover      = "/uploads/seed/ws-3dp-1.jpg",
                Hours      = "السبت – الخميس: 9ص – 7م",
                Capacity   = "حتى 150 قطعة / أسبوع"
            },
            new
            {
                Email      = "maker1@dmp.jo",
                Workshop   = "ورشة الإبداع",
                City       = "إربد",
                Address    = "شارع الجامعة، مجمع الإبداع التجاري، الطابق الأول",
                Bio        = "متخصصون في قطع الليزر والأكريليك والخشب. نوفر خدمة التوصيل لجميع محافظات الأردن. خبرة 6 سنوات في تنفيذ مشاريع اللافتات التجارية والهدايا المخصصة والديكورات الداخلية.",
                Phone      = "0782345678",
                Delivery   = DeliveryOption.Nationwide,
                IsApproved = true,
                Lat        = 32.5556,
                Lng        = 35.8500,
                Category   = MachineCategory.LaserCutting,
                Machine    = "xTool D1 Pro 20W",
                MachineDesc= "ليزر 20W عالي الدقة للقطع والنقش على الأكريليك والخشب والجلد",
                Materials  = "Acrylic, Wood, Leather, Cardboard, Fabric",
                Profile    = "/uploads/seed/profile-maker1.jpg",
                Cover      = "/uploads/seed/ws-laser-1.jpg",
                Hours      = "السبت – الجمعة: 8ص – 8م",
                Capacity   = "حتى 200 قطعة / أسبوع"
            },
            new
            {
                Email      = "maker2@dmp.jo",
                Workshop   = "تك فاب",
                City       = "الزرقاء",
                Address    = "المنطقة الصناعية الأولى، الزرقاء، ورقم 7",
                Bio        = "نقدم خدمات تشغيل CNC وتفريز المعادن للقطاع الصناعي والأفراد. دقة عالية تصل إلى 0.01mm. نتعامل مع الألومنيوم والفولاذ المقاون والنحاس والبلاستيك. موثوق من قِبل أكثر من 50 شركة صناعية في الأردن.",
                Phone      = "0773456789",
                Delivery   = DeliveryOption.LocalDelivery,
                IsApproved = true,
                Lat        = 32.0728,
                Lng        = 36.0875,
                Category   = MachineCategory.CNC,
                Machine    = "Haas Mini Mill",
                MachineDesc= "مركز تشغيل CNC ثلاثي المحاور — دقة 0.01mm، مناسب للمعادن والبلاستيك",
                Materials  = "Aluminum, Stainless Steel, Brass, Copper, Nylon, Acetal",
                Profile    = "/uploads/seed/profile-maker2.jpg",
                Cover      = "/uploads/seed/cover-cnc.jpg",
                Hours      = "الأحد – الخميس: 7ص – 5م",
                Capacity   = "حتى 80 قطعة معقدة / أسبوع"
            },
            new
            {
                Email      = "maker3@dmp.jo",
                Workshop   = "ورشة الخليج للنجارة",
                City       = "العقبة",
                Address    = "منطقة الخدمات الصناعية، العقبة، بالقرب من الميناء",
                Bio        = "ورشة نجارة متقدمة مع راوتر CNC للخشب. نصنع الأثاث والديكور الداخلي حسب الطلب. استخدام أجود أنواع الخشب الطبيعي والمركب. تجهيز المطاعم والفنادق والمكاتب.",
                Phone      = "0764567890",
                Delivery   = DeliveryOption.PickupOnly,
                IsApproved = false,
                Lat        = 29.5266,
                Lng        = 35.0078,
                Category   = MachineCategory.Woodwork,
                Machine    = "ShopBot PRSalpha 96×48",
                MachineDesc= "راوتر CNC للخشب — مساحة عمل 2.4×1.2م، مثالي للقطع والنقش ثلاثي الأبعاد",
                Materials  = "Oak, Pine, MDF, Plywood, Walnut",
                Profile    = "/uploads/seed/profile-maker3.jpg",
                Cover      = "/uploads/seed/cover-wood.jpg",
                Hours      = "السبت – الخميس: 8ص – 6م",
                Capacity   = "5-10 قطع أثاث / أسبوع"
            },
            new
            {
                Email      = "maker4@dmp.jo",
                Workshop   = "الريادة للتصنيع الإلكتروني",
                City       = "عمّان",
                Address    = "مدينة الحسين للأعمال، برج C، الطابق 3",
                Bio        = "متخصصون في تصنيع لوحات PCB ونماذج إلكترونية للشركات الناشئة والمطورين. نوفر خدمة التصميم الكامل من الفكرة حتى النموذج الأول. شركاء موثوقون لأكثر من 30 ستارتاب أردني.",
                Phone      = "0795678901",
                Delivery   = DeliveryOption.Nationwide,
                IsApproved = true,
                Lat        = 31.9800,
                Lng        = 35.9350,
                Category   = MachineCategory.PCB,
                Machine    = "LPKF ProtoMat S104",
                MachineDesc= "ماكينة تصنيع PCB احترافية — طبقتان، دقة 100µm، مناسبة للـ SMD",
                Materials  = "FR4, Aluminum PCB, Flexible PCB",
                Profile    = "/uploads/seed/profile-maker4.jpg",
                Cover      = "/uploads/seed/cover-pcb.jpg",
                Hours      = "الأحد – الخميس: 9ص – 6م",
                Capacity   = "حتى 50 لوحة / أسبوع"
            },
        };

        foreach (var p in profiles)
        {
            var user = await um.FindByEmailAsync(p.Email);
            if (user == null) continue;

            if (await db.Manufacturers.AnyAsync(m => m.UserId == user.Id)) continue;

            var mfr = new Manufacturer
            {
                UserId            = user.Id,
                WorkshopName      = p.Workshop,
                City              = p.City,
                Address           = p.Address,
                Bio               = p.Bio,
                ContactPhone      = p.Phone,
                ContactEmail      = p.Email,
                Delivery          = p.Delivery,
                IsApproved        = p.IsApproved,
                Latitude          = p.Lat,
                Longitude         = p.Lng,
                WorkingHours      = p.Hours,
                ProductionCapacity= p.Capacity,
                ProfilePhotoPath  = p.Profile,
                CoverPhotoPath    = p.Cover,
            };

            db.Manufacturers.Add(mfr);
            await db.SaveChangesAsync();

            db.Machines.Add(new Machine
            {
                ManufacturerId    = mfr.Id,
                Category          = p.Category,
                ModelName         = p.Machine,
                Description       = p.MachineDesc,
                SupportedMaterials= p.Materials,
                PricingNote       = "تواصل للحصول على عرض سعر مخصص"
            });
            await db.SaveChangesAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────
    private static async Task EnsureReviews(
        ApplicationDbContext db,
        UserManager<ApplicationUser> um)
    {
        if (await db.Reviews.AnyAsync()) return;

        var ali   = await um.FindByEmailAsync("ali@dmp.jo");
        var sara  = await um.FindByEmailAsync("sara@dmp.jo");
        var omar  = await um.FindByEmailAsync("omar@dmp.jo");
        var reem  = await um.FindByEmailAsync("reem@dmp.jo");
        var khaled= await um.FindByEmailAsync("khaled@dmp.jo");
        var nour  = await um.FindByEmailAsync("nour@dmp.jo");

        var workshop = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "workshop@dmp.jo");
        var maker1   = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "maker1@dmp.jo");
        var maker2   = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "maker2@dmp.jo");
        var maker4   = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "maker4@dmp.jo");

        var reviews = new List<Review>();

        // ورشة المصنّع — 5 تقييمات
        if (workshop != null)
        {
            if (ali != null) reviews.Add(new Review { ManufacturerId = workshop.Id, CustomerUserId = ali.Id, CustomerName = "علي الأحمد", Rating = 5, Comment = "طباعة ممتازة وسريعة. الدقة كانت مذهلة في النموذج الهندسي الذي طلبته. سأتعامل معهم مرة أخرى بالتأكيد.", CreatedAt = DateTime.UtcNow.AddDays(-30) });
            if (sara != null) reviews.Add(new Review { ManufacturerId = workshop.Id, CustomerUserId = sara.Id, CustomerName = "سارة المنصور", Rating = 4, Comment = "جودة الطباعة عالية جداً والتسليم كان في الموعد. الأسعار معقولة مقارنة بالجودة المقدمة.", CreatedAt = DateTime.UtcNow.AddDays(-22) });
            if (omar != null) reviews.Add(new Review { ManufacturerId = workshop.Id, CustomerUserId = omar.Id, CustomerName = "عمر الخالدي", Rating = 5, Comment = "أفضل ورشة طباعة ثلاثية الأبعاد في عمّان. محمد محترف جداً ويفهم متطلبات العميل بدقة.", CreatedAt = DateTime.UtcNow.AddDays(-15) });
            if (reem != null) reviews.Add(new Review { ManufacturerId = workshop.Id, CustomerUserId = reem.Id, CustomerName = "ريم العمري", Rating = 5, Comment = "طبعت معهم مجسم معماري لمشروع التخرج. النتيجة فاقت التوقعات بكثير! شكراً.", CreatedAt = DateTime.UtcNow.AddDays(-8) });
            if (khaled != null) reviews.Add(new Review { ManufacturerId = workshop.Id, CustomerUserId = khaled.Id, CustomerName = "خالد الزعبي", Rating = 4, Comment = "خدمة ممتازة وتواصل سريع. التعديلات على التصميم تمت بدون مشاكل.", CreatedAt = DateTime.UtcNow.AddDays(-3) });
        }

        // ورشة الإبداع — 4 تقييمات
        if (maker1 != null)
        {
            if (reem != null) reviews.Add(new Review { ManufacturerId = maker1.Id, CustomerUserId = reem.Id, CustomerName = "ريم العمري", Rating = 5, Comment = "لافتات الأكريليك خرجت رائعة ومضيئة جداً. الشعار طلع واضح وجميل. أنصح الجميع بالتعامل مع ورشة الإبداع.", CreatedAt = DateTime.UtcNow.AddDays(-25) });
            if (ali != null) reviews.Add(new Review { ManufacturerId = maker1.Id, CustomerUserId = ali.Id, CustomerName = "علي الأحمد", Rating = 4, Comment = "عمل دقيق وأسعار منافسة. التوصيل لعمّان كان سريع. الجودة ممتازة في النقش.", CreatedAt = DateTime.UtcNow.AddDays(-18) });
            if (nour != null) reviews.Add(new Review { ManufacturerId = maker1.Id, CustomerUserId = nour.Id, CustomerName = "نور الشريف", Rating = 5, Comment = "طلبت هدايا خشبية منقوشة لحفل الشركة. كانت راقية جداً وتسليمها قبل الموعد. شكراً أحمد!", CreatedAt = DateTime.UtcNow.AddDays(-10) });
            if (khaled != null) reviews.Add(new Review { ManufacturerId = maker1.Id, CustomerUserId = khaled.Id, CustomerName = "خالد الزعبي", Rating = 4, Comment = "تقطيع الأكريليك دقيق جداً والألوان زاهية. التسليم الوطني ممتاز.", CreatedAt = DateTime.UtcNow.AddDays(-5) });
        }

        // تك فاب — 3 تقييمات
        if (maker2 != null)
        {
            if (sara != null) reviews.Add(new Review { ManufacturerId = maker2.Id, CustomerUserId = sara.Id, CustomerName = "سارة المنصور", Rating = 5, Comment = "دقة عالية جداً في تشغيل الألومنيوم. القطع طلعت مطابقة تماماً للرسم الهندسي. محترفون فعلاً.", CreatedAt = DateTime.UtcNow.AddDays(-20) });
            if (omar != null) reviews.Add(new Review { ManufacturerId = maker2.Id, CustomerUserId = omar.Id, CustomerName = "عمر الخالدي", Rating = 4, Comment = "كريم محترف ويفهم في المعادن. الأسعار عادلة والجودة ممتازة. الورشة مجهزة بأحدث المعدات.", CreatedAt = DateTime.UtcNow.AddDays(-12) });
            if (ali != null) reviews.Add(new Review { ManufacturerId = maker2.Id, CustomerUserId = ali.Id, CustomerName = "علي الأحمد", Rating = 5, Comment = "ثالث مرة أتعامل معهم. دائماً منضبطون بالمواعيد والجودة لا تتغير. أنصح بهم لأي مشروع صناعي.", CreatedAt = DateTime.UtcNow.AddDays(-2) });
        }

        // الريادة للتصنيع الإلكتروني — 3 تقييمات
        if (maker4 != null)
        {
            if (ali != null) reviews.Add(new Review { ManufacturerId = maker4.Id, CustomerUserId = ali.Id, CustomerName = "علي الأحمد", Rating = 5, Comment = "لوحات PCB ممتازة للمشروع الإلكتروني. يوسف ساعدني في مراجعة التصميم قبل التصنيع وهذا وفّر عليّ أخطاء كثيرة.", CreatedAt = DateTime.UtcNow.AddDays(-35) });
            if (reem != null) reviews.Add(new Review { ManufacturerId = maker4.Id, CustomerUserId = reem.Id, CustomerName = "ريم العمري", Rating = 4, Comment = "جودة تصنيع عالية والتسليم كان سريع. اللوحات اشتغلت من أول تجربة بدون مشاكل.", CreatedAt = DateTime.UtcNow.AddDays(-14) });
            if (nour != null) reviews.Add(new Review { ManufacturerId = maker4.Id, CustomerUserId = nour.Id, CustomerName = "نور الشريف", Rating = 5, Comment = "أفضل تجربة في تصنيع PCB في الأردن. السرعة والدقة والدعم الفني كلها ممتازة.", CreatedAt = DateTime.UtcNow.AddDays(-7) });
        }

        db.Reviews.AddRange(reviews);
        await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────
    private static async Task EnsurePortfolioAndGallery(
        ApplicationDbContext db,
        UserManager<ApplicationUser> um)
    {
        if (await db.PortfolioItems.AnyAsync()) return;

        var workshop = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "workshop@dmp.jo");
        var maker1   = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "maker1@dmp.jo");
        var maker2   = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "maker2@dmp.jo");
        var maker3   = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "maker3@dmp.jo");
        var maker4   = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "maker4@dmp.jo");

        // ── Portfolio Items ───────────────────────────────────────
        var portfolio = new List<PortfolioItem>();

        if (workshop != null)
        {
            portfolio.AddRange(new[]
            {
                new PortfolioItem { ManufacturerId = workshop.Id, Title = "مجسم معماري لمشروع سكني", Description = "طباعة مجسم معماري دقيق لمجمع سكني في عمّان. المقياس 1:100، مدة التنفيذ 3 أيام.", Category = MachineCategory.Printing3D, ImagePath = "/uploads/seed/port-3dp-1.jpg", CompletedAt = DateTime.UtcNow.AddDays(-60) },
                new PortfolioItem { ManufacturerId = workshop.Id, Title = "نماذج بروتوتايب لشركة ستارتاب", Description = "تصنيع 5 نماذج أولية لمنتج جديد. عدة تعديلات حتى الوصول للتصميم النهائي.", Category = MachineCategory.Printing3D, ImagePath = "/uploads/seed/port-3dp-2.jpg", CompletedAt = DateTime.UtcNow.AddDays(-40) },
                new PortfolioItem { ManufacturerId = workshop.Id, Title = "أجزاء ميكانيكية بديلة", Description = "طباعة قطع غيار بديلة لآلة صناعية متوقفة عن الإنتاج. وفّرت على العميل 80% من تكلفة الاستيراد.", Category = MachineCategory.Printing3D, ImagePath = "/uploads/seed/port-3dp-2.jpg", CompletedAt = DateTime.UtcNow.AddDays(-20) },
            });
        }

        if (maker1 != null)
        {
            portfolio.AddRange(new[]
            {
                new PortfolioItem { ManufacturerId = maker1.Id, Title = "لافتة محل عطور — أكريليك مضيء", Description = "لافتة بحروف بارزة مضيئة بـ LED. أكريليك شفاف مع إضاءة زرقاء. حجم 100×50 سم.", Category = MachineCategory.LaserCutting, ImagePath = "/uploads/seed/port-laser-1.jpg", CompletedAt = DateTime.UtcNow.AddDays(-50) },
                new PortfolioItem { ManufacturerId = maker1.Id, Title = "هدايا خشبية منقوشة — حفل شركة", Description = "100 قطعة خشبية منقوشة بشعار الشركة وأسماء الموظفين. خشب الزان الطبيعي.", Category = MachineCategory.LaserCutting, ImagePath = "/uploads/seed/ws-laser-1.jpg", CompletedAt = DateTime.UtcNow.AddDays(-35) },
                new PortfolioItem { ManufacturerId = maker1.Id, Title = "بطاقات دعوة زفاف ليزر", Description = "300 بطاقة دعوة بتصميم مخصص، قطع ليزر على ورق مقوى فاخر مع حرق الاسم.", Category = MachineCategory.LaserCutting, ImagePath = "/uploads/seed/ws-laser-2.jpg", CompletedAt = DateTime.UtcNow.AddDays(-15) },
            });
        }

        if (maker2 != null)
        {
            portfolio.AddRange(new[]
            {
                new PortfolioItem { ManufacturerId = maker2.Id, Title = "قطع ألومنيوم للصناعة الغذائية", Description = "تصنيع 50 قطعة ألومنيوم للاستخدام في خط إنتاج غذائي. دقة 0.02mm، مواد grade 6061.", Category = MachineCategory.CNC, ImagePath = "/uploads/seed/port-cnc-1.jpg", CompletedAt = DateTime.UtcNow.AddDays(-55) },
                new PortfolioItem { ManufacturerId = maker2.Id, Title = "قاعدة معدنية لآلة تعبئة", Description = "تصنيع قاعدة ستانلس ستيل مخصصة لآلة تعبئة صناعية. مقاومة للتآكل والحرارة.", Category = MachineCategory.CNC, ImagePath = "/uploads/seed/port-cnc-2.jpg", CompletedAt = DateTime.UtcNow.AddDays(-28) },
            });
        }

        if (maker3 != null)
        {
            portfolio.AddRange(new[]
            {
                new PortfolioItem { ManufacturerId = maker3.Id, Title = "مكاتب هوم أوفيس — تصميم عصري", Description = "تصنيع 8 مكاتب خشبية بتصميم عصري لشركة ناشئة في العقبة. خشب البلوط الطبيعي.", Category = MachineCategory.Woodwork, ImagePath = "/uploads/seed/port-wood-1.jpg", CompletedAt = DateTime.UtcNow.AddDays(-45) },
                new PortfolioItem { ManufacturerId = maker3.Id, Title = "ديكور مطعم — لوحات خشبية منحوتة", Description = "لوحات ديكورية خشبية بنقوش عربية تقليدية لمطعم فاخر. خشب الجوز الطبيعي.", Category = MachineCategory.Woodwork, ImagePath = "/uploads/seed/ws-wood-1.jpg", CompletedAt = DateTime.UtcNow.AddDays(-25) },
            });
        }

        if (maker4 != null)
        {
            portfolio.AddRange(new[]
            {
                new PortfolioItem { ManufacturerId = maker4.Id, Title = "لوحة تحكم للمنزل الذكي", Description = "تصنيع لوحة PCB للتحكم في الإضاءة والتكييف عبر التطبيق. 4 طبقات، مكونات SMD دقيقة.", Category = MachineCategory.PCB, ImagePath = "/uploads/seed/port-pcb-1.jpg", CompletedAt = DateTime.UtcNow.AddDays(-65) },
                new PortfolioItem { ManufacturerId = maker4.Id, Title = "وحدة قياس جودة الهواء", Description = "لوحة PCB مخصصة لقياس CO2 والرطوبة والحرارة. مستخدمة في شبكة محطات رصد بيئي أردنية.", Category = MachineCategory.PCB, ImagePath = "/uploads/seed/port-pcb-2.jpg", CompletedAt = DateTime.UtcNow.AddDays(-30) },
                new PortfolioItem { ManufacturerId = maker4.Id, Title = "نظام إدارة بطاريات (BMS)", Description = "لوحة BMS مخصصة لحزمة بطاريات ليثيوم 48V لدراجة كهربائية أردنية الصنع.", Category = MachineCategory.PCB, ImagePath = "/uploads/seed/port-pcb-1.jpg", CompletedAt = DateTime.UtcNow.AddDays(-10) },
            });
        }

        db.PortfolioItems.AddRange(portfolio);
        await db.SaveChangesAsync();

        // ── Workshop Gallery Photos ───────────────────────────────
        if (await db.WorkshopPhotos.AnyAsync()) return;

        var galleryPhotos = new List<WorkshopPhoto>();

        if (workshop != null)
        {
            galleryPhotos.Add(new WorkshopPhoto { ManufacturerId = workshop.Id, FilePath = "/uploads/seed/ws-3dp-1.jpg", UploadedAt = DateTime.UtcNow.AddDays(-90) });
            galleryPhotos.Add(new WorkshopPhoto { ManufacturerId = workshop.Id, FilePath = "/uploads/seed/ws-3dp-2.jpg", UploadedAt = DateTime.UtcNow.AddDays(-60) });
        }
        if (maker1 != null)
        {
            galleryPhotos.Add(new WorkshopPhoto { ManufacturerId = maker1.Id, FilePath = "/uploads/seed/ws-laser-1.jpg", UploadedAt = DateTime.UtcNow.AddDays(-80) });
            galleryPhotos.Add(new WorkshopPhoto { ManufacturerId = maker1.Id, FilePath = "/uploads/seed/ws-laser-2.jpg", UploadedAt = DateTime.UtcNow.AddDays(-45) });
        }
        if (maker2 != null)
        {
            galleryPhotos.Add(new WorkshopPhoto { ManufacturerId = maker2.Id, FilePath = "/uploads/seed/ws-cnc-1.jpg", UploadedAt = DateTime.UtcNow.AddDays(-70) });
            galleryPhotos.Add(new WorkshopPhoto { ManufacturerId = maker2.Id, FilePath = "/uploads/seed/ws-cnc-2.jpg", UploadedAt = DateTime.UtcNow.AddDays(-30) });
        }
        if (maker3 != null)
        {
            galleryPhotos.Add(new WorkshopPhoto { ManufacturerId = maker3.Id, FilePath = "/uploads/seed/ws-wood-1.jpg", UploadedAt = DateTime.UtcNow.AddDays(-50) });
        }
        if (maker4 != null)
        {
            galleryPhotos.Add(new WorkshopPhoto { ManufacturerId = maker4.Id, FilePath = "/uploads/seed/ws-pcb-1.jpg", UploadedAt = DateTime.UtcNow.AddDays(-40) });
        }

        db.WorkshopPhotos.AddRange(galleryPhotos);
        await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────
    private static async Task EnsureRequests(
        ApplicationDbContext db,
        UserManager<ApplicationUser> um)
    {
        if (await db.ManufacturingRequests.AnyAsync()) return;

        var ali   = await um.FindByEmailAsync("ali@dmp.jo");
        var sara  = await um.FindByEmailAsync("sara@dmp.jo");
        var omar  = await um.FindByEmailAsync("omar@dmp.jo");
        var reem  = await um.FindByEmailAsync("reem@dmp.jo");
        var khaled= await um.FindByEmailAsync("khaled@dmp.jo");
        var nour  = await um.FindByEmailAsync("nour@dmp.jo");

        var workshop = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "workshop@dmp.jo");
        var maker1   = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "maker1@dmp.jo");
        var maker2   = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "maker2@dmp.jo");
        var maker4   = await db.Manufacturers.FirstOrDefaultAsync(m => m.ContactEmail == "maker4@dmp.jo");

        // ─── طلب 1: مكتمل + مدفوع (علي → ورشة المصنّع) ───────────
        if (ali != null && workshop != null)
        {
            var req1 = new ManufacturingRequest
            {
                CustomerId     = ali.Id,
                ManufacturerId = workshop.Id,
                Title          = "طباعة 15 نموذج هندسي لمشروع التخرج",
                Description    = "أحتاج طباعة 15 نموذج هندسي بمواصفات دقيقة لمشروع تخرج هندسة ميكانيكية. المواد: PLA رمادي، دقة عالية 0.1mm، بدون دعامات قدر الإمكان.",
                Category       = MachineCategory.Printing3D,
                Material       = "PLA رمادي",
                Quantity       = 15,
                Budget         = 180,
                Status         = RequestStatus.Completed,
                TrackingStage  = Models.TrackingStage.Delivered,
                PaymentStatus  = PaymentStatus.Paid,
                PaidAmount     = 135,
                PaidAt         = DateTime.UtcNow.AddDays(-10),
                CreatedAt      = DateTime.UtcNow.AddDays(-35),
            };
            db.ManufacturingRequests.Add(req1);
            await db.SaveChangesAsync();

            db.Quotations.Add(new Quotation { RequestId = req1.Id, ManufacturerId = workshop.Id, Price = 135, EstimatedDays = 4, Notes = "سيتم التنفيذ بدقة 0.1mm على Ender 5 Pro. السعر يشمل جميع القطع.", Status = QuotationStatus.Accepted, CreatedAt = DateTime.UtcNow.AddDays(-33) });
            db.OrderUpdates.AddRange(
                new OrderUpdate { RequestId = req1.Id, Stage = Models.TrackingStage.Received,        Note = "تم استلام الطلب والملفات بنجاح",              CreatedAt = DateTime.UtcNow.AddDays(-32) },
                new OrderUpdate { RequestId = req1.Id, Stage = Models.TrackingStage.InManufacturing, Note = "بدأت الطباعة — متوقع الانتهاء خلال يومين",     CreatedAt = DateTime.UtcNow.AddDays(-28) },
                new OrderUpdate { RequestId = req1.Id, Stage = Models.TrackingStage.ReadyForPickup,  Note = "جميع القطع جاهزة وتم فحص الجودة",              CreatedAt = DateTime.UtcNow.AddDays(-12) },
                new OrderUpdate { RequestId = req1.Id, Stage = Models.TrackingStage.Delivered,       Note = "تم التسليم للعميل. شكراً لتعاملكم مع ورشة المصنّع", CreatedAt = DateTime.UtcNow.AddDays(-10) }
            );
            await db.SaveChangesAsync();
        }

        // ─── طلب 2: مكتمل + مدفوع (سارة → ورشة الإبداع) ──────────
        if (sara != null && maker1 != null)
        {
            var req2 = new ManufacturingRequest
            {
                CustomerId     = sara.Id,
                ManufacturerId = maker1.Id,
                Title          = "لافتة أكريليك مضيئة لمحل تجميل",
                Description    = "لافتة خارجية لمحل تجميل. الاسم: صالون لمسة. أكريليك أبيض مع إضاءة LED وردية من الخلف. الحجم: 80×40 سم.",
                Category       = MachineCategory.LaserCutting,
                Material       = "أكريليك 5mm + LED",
                Quantity       = 1,
                Budget         = 120,
                Status         = RequestStatus.Completed,
                TrackingStage  = Models.TrackingStage.Delivered,
                PaymentStatus  = PaymentStatus.Paid,
                PaidAmount     = 95,
                PaidAt         = DateTime.UtcNow.AddDays(-5),
                CreatedAt      = DateTime.UtcNow.AddDays(-25),
            };
            db.ManufacturingRequests.Add(req2);
            await db.SaveChangesAsync();

            db.Quotations.Add(new Quotation { RequestId = req2.Id, ManufacturerId = maker1.Id, Price = 95, EstimatedDays = 3, Notes = "التصميم سيرسل للمراجعة قبل القطع. السعر يشمل الإضاءة والتركيب.", Status = QuotationStatus.Accepted, CreatedAt = DateTime.UtcNow.AddDays(-24) });
            db.OrderUpdates.AddRange(
                new OrderUpdate { RequestId = req2.Id, Stage = Models.TrackingStage.Received,        Note = "تم استلام الطلب وموافقة العميل على التصميم",      CreatedAt = DateTime.UtcNow.AddDays(-23) },
                new OrderUpdate { RequestId = req2.Id, Stage = Models.TrackingStage.InManufacturing, Note = "جاري القطع والتجميع مع الإضاءة",                  CreatedAt = DateTime.UtcNow.AddDays(-20) },
                new OrderUpdate { RequestId = req2.Id, Stage = Models.TrackingStage.Delivered,       Note = "تم التسليم والتركيب في الموقع",                    CreatedAt = DateTime.UtcNow.AddDays(-5) }
            );
            await db.SaveChangesAsync();
        }

        // ─── طلب 3: جاري التنفيذ (عمر → تك فاب) ───────────────────
        if (omar != null && maker2 != null)
        {
            var req3 = new ManufacturingRequest
            {
                CustomerId     = omar.Id,
                ManufacturerId = maker2.Id,
                Title          = "قطع ألومنيوم مخصصة للورشة الميكانيكية",
                Description    = "أحتاج 20 قطعة ألومنيوم مشغولة CNC حسب الرسم الهندسي المرفق. الخامة: ألومنيوم 6061، دقة ±0.05mm. للاستخدام في آلة تصنيع خاصة.",
                Category       = MachineCategory.CNC,
                Material       = "ألومنيوم 6061",
                Quantity       = 20,
                Budget         = 800,
                Status         = RequestStatus.InProgress,
                TrackingStage  = Models.TrackingStage.InManufacturing,
                PaymentStatus  = PaymentStatus.Pending,
                CreatedAt      = DateTime.UtcNow.AddDays(-12),
            };
            db.ManufacturingRequests.Add(req3);
            await db.SaveChangesAsync();

            db.Quotations.Add(new Quotation { RequestId = req3.Id, ManufacturerId = maker2.Id, Price = 720, EstimatedDays = 7, Notes = "السعر يشمل المادة الخام والتشغيل والفحص. قد يزيد 10% إذا وجدت تعقيدات في الرسم.", Status = QuotationStatus.Accepted, CreatedAt = DateTime.UtcNow.AddDays(-11) });
            db.OrderUpdates.AddRange(
                new OrderUpdate { RequestId = req3.Id, Stage = Models.TrackingStage.Received,        Note = "تم استلام الرسم الهندسي وتأكيد المواصفات", CreatedAt = DateTime.UtcNow.AddDays(-10) },
                new OrderUpdate { RequestId = req3.Id, Stage = Models.TrackingStage.InManufacturing, Note = "بدأ التشغيل — 8 قطع مكتملة من أصل 20",    CreatedAt = DateTime.UtcNow.AddDays(-5) }
            );
            await db.SaveChangesAsync();
        }

        // ─── طلب 4: جاري التنفيذ (ريم → الريادة) ──────────────────
        if (reem != null && maker4 != null)
        {
            var req4 = new ManufacturingRequest
            {
                CustomerId     = reem.Id,
                ManufacturerId = maker4.Id,
                Title          = "لوحة PCB لنظام ري زراعي ذكي",
                Description    = "لوحة PCB للتحكم في نظام ري آلي للزراعة المائية. تشمل: قارئ رطوبة تربة، مضخة مياه، وحدة WiFi ESP32، شاشة OLED. ملف Gerber مرفق.",
                Category       = MachineCategory.PCB,
                Material       = "FR4 ذو طبقتين",
                Quantity       = 5,
                Budget         = 300,
                Status         = RequestStatus.InProgress,
                TrackingStage  = Models.TrackingStage.Received,
                PaymentStatus  = PaymentStatus.Pending,
                CreatedAt      = DateTime.UtcNow.AddDays(-6),
            };
            db.ManufacturingRequests.Add(req4);
            await db.SaveChangesAsync();

            db.Quotations.Add(new Quotation { RequestId = req4.Id, ManufacturerId = maker4.Id, Price = 250, EstimatedDays = 5, Notes = "سيتم مراجعة ملف Gerber وإرسال تقرير DRC قبل البدء بالتصنيع.", Status = QuotationStatus.Accepted, CreatedAt = DateTime.UtcNow.AddDays(-5) });
            db.OrderUpdates.Add(new OrderUpdate { RequestId = req4.Id, Stage = Models.TrackingStage.Received, Note = "تم استلام ملف Gerber وجاري مراجعة التصميم", CreatedAt = DateTime.UtcNow.AddDays(-4) });
            await db.SaveChangesAsync();
        }

        // ─── طلب 5: مفتوح — عروض أسعار متعددة (خالد) ──────────────
        if (khaled != null)
        {
            var req5 = new ManufacturingRequest
            {
                CustomerId  = khaled.Id,
                Title       = "نقش ليزر على 50 قلم معدني — هدايا شركة",
                Description = "أحتاج نقش شعار شركة (سيرسل SVG) على 50 قلم معدني. النقش يشمل الشعار والنص: \"شكراً لشراكتكم — 2026\". الأقلام ستوفر من قِبلي.",
                Category    = MachineCategory.LaserCutting,
                Material    = "معدن (أقلام جاهزة)",
                Quantity    = 50,
                Budget      = 200,
                Status      = RequestStatus.Open,
                CreatedAt   = DateTime.UtcNow.AddDays(-4),
            };
            db.ManufacturingRequests.Add(req5);
            await db.SaveChangesAsync();

            if (maker1 != null)
            {
                db.Quotations.Add(new Quotation { RequestId = req5.Id, ManufacturerId = maker1.Id, Price = 150, EstimatedDays = 2, Notes = "لدينا خبرة في النقش على المعادن. الأسعار تشمل الإعداد والتجربة. يمكن التسليم خلال يومين.", Status = QuotationStatus.Pending, CreatedAt = DateTime.UtcNow.AddDays(-3) });
            }
            if (workshop != null)
            {
                db.Quotations.Add(new Quotation { RequestId = req5.Id, ManufacturerId = workshop.Id, Price = 175, EstimatedDays = 3, Notes = "نستخدم ليزر CO2 للنقش على المعادن. النتيجة احترافية وثابتة. يشمل السعر التغليف.", Status = QuotationStatus.Pending, CreatedAt = DateTime.UtcNow.AddDays(-2) });
            }
            await db.SaveChangesAsync();
        }

        // ─── طلب 6: مفتوح — بدون عروض بعد (نور) ───────────────────
        if (nour != null)
        {
            var req6 = new ManufacturingRequest
            {
                CustomerId  = nour.Id,
                Title       = "طباعة ثلاثية الأبعاد — 30 علبة تغليف مخصصة",
                Description = "أحتاج طباعة 30 علبة تغليف لمنتج عضوي. الأبعاد: 10×8×5 سم. المادة: PLA أبيض قابل للطعام. التصميم موجود كـ STL. مطلوب تشطيب ناعم.",
                Category    = MachineCategory.Printing3D,
                Material    = "PLA Food-Safe أبيض",
                Quantity    = 30,
                Budget      = 250,
                Status      = RequestStatus.Open,
                CreatedAt   = DateTime.UtcNow.AddDays(-2),
            };
            db.ManufacturingRequests.Add(req6);
            await db.SaveChangesAsync();
        }

        // ─── طلب 7: مفتوح — عرض سعر مرفوض ثم عرض جديد (علي) ──────
        if (ali != null && maker2 != null && workshop != null)
        {
            var req7 = new ManufacturingRequest
            {
                CustomerId  = ali.Id,
                Title       = "أجزاء بلاستيكية CNC لجهاز طبي",
                Description = "تصنيع 10 قطع بلاستيكية (Nylon PA6) بدقة عالية لجهاز طبي. الرسم الهندسي متاح. تنظيف حواف واضح ومطلوب.",
                Category    = MachineCategory.CNC,
                Material    = "Nylon PA6",
                Quantity    = 10,
                Budget      = 500,
                Status      = RequestStatus.Open,
                CreatedAt   = DateTime.UtcNow.AddDays(-8),
            };
            db.ManufacturingRequests.Add(req7);
            await db.SaveChangesAsync();

            db.Quotations.AddRange(
                new Quotation { RequestId = req7.Id, ManufacturerId = maker2.Id, Price = 480, EstimatedDays = 5, Notes = "لدينا خبرة في تشغيل Nylon للتطبيقات الطبية. يشمل التصنيع والغسيل.", Status = QuotationStatus.Rejected, CreatedAt = DateTime.UtcNow.AddDays(-7) },
                new Quotation { RequestId = req7.Id, ManufacturerId = workshop.Id, Price = 420, EstimatedDays = 6, Notes = "يمكننا التشغيل بدقة 0.05mm. هل يمكن مشاركة المواصفات الكاملة؟", Status = QuotationStatus.Pending, CreatedAt = DateTime.UtcNow.AddDays(-5) }
            );
            await db.SaveChangesAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────
    private static async Task EnsureCampaigns(
        ApplicationDbContext db,
        UserManager<ApplicationUser> um)
    {
        if (await db.GroupBuyingCampaigns.AnyAsync()) return;

        var maker1   = await um.FindByEmailAsync("maker1@dmp.jo");
        var maker2   = await um.FindByEmailAsync("maker2@dmp.jo");
        var maker4   = await um.FindByEmailAsync("maker4@dmp.jo");
        var workshop = await um.FindByEmailAsync("workshop@dmp.jo");

        var campaigns = new List<GroupBuyingCampaign>
        {
            new()
            {
                Title           = "طباعة ثلاثية الأبعاد — نماذج هندسية PLA",
                Description     = "حملة شراء جماعي لطباعة نماذج هندسية بمادة PLA عالية الجودة. مناسبة للمهندسين والطلاب والمصممين. الطلب الأدنى 50 قطعة للحصول على سعر الجملة. مدة التنفيذ 5 أيام عمل بعد إغلاق الحملة.",
                ItemName        = "نموذج هندسي PLA (10×10×5 سم)",
                IndividualPrice = 15,
                GroupPrice      = 9,
                MinQuantity     = 50,
                CurrentQuantity = 31,
                Status          = CampaignStatus.Active,
                DeadlineDate    = DateTime.UtcNow.AddDays(18),
            },
            new()
            {
                Title           = "لافتات أكريليك مضيئة للمحلات",
                Description     = "لافتات أكريليك مضيئة بقطع ليزر عالي الدقة. مثالية للمحلات التجارية والمكاتب والعيادات. التخصيص متاح: الاسم، الشعار، اللون. الحجم الافتراضي 40×30 سم.",
                ItemName        = "لافتة أكريليك مضيئة 40×30 سم",
                IndividualPrice = 45,
                GroupPrice      = 28,
                MinQuantity     = 30,
                CurrentQuantity = 30,
                Status          = CampaignStatus.AwaitingPayment,
                DeadlineDate    = DateTime.UtcNow.AddDays(3),
            },
            new()
            {
                Title           = "قطع ألومنيوم CNC للاستخدام الصناعي",
                Description     = "قطع ألومنيوم 6061 مشغولة بـ CNC للاستخدام الصناعي والميكانيكي. دقة عالية حتى 0.05mm. مناسبة لشركات التصنيع والمصانع الصغيرة والورش الميكانيكية.",
                ItemName        = "قطعة ألومنيوم CNC مخصصة (رسم مرفق)",
                IndividualPrice = 80,
                GroupPrice      = 52,
                MinQuantity     = 20,
                CurrentQuantity = 8,
                Status          = CampaignStatus.Active,
                DeadlineDate    = DateTime.UtcNow.AddDays(25),
            },
            new()
            {
                Title           = "لوحات PCB بروتوتايب — خاصة بالمطورين",
                Description     = "تصنيع لوحات PCB مخصصة للمطورين وشركات الإلكترونيات. طبقتان، مواصفات احترافية، تسليم Gerber. مناسبة للستارتاب وعشاق الإلكترونيات وطلاب الهندسة.",
                ItemName        = "لوحة PCB مخصصة 10×10 سم (2 طبقات)",
                IndividualPrice = 35,
                GroupPrice      = 20,
                MinQuantity     = 40,
                CurrentQuantity = 12,
                Status          = CampaignStatus.Active,
                DeadlineDate    = DateTime.UtcNow.AddDays(30),
            },
            new()
            {
                Title           = "مكاتب هوم أوفيس خشب طبيعي CNC",
                Description     = "مكاتب هوم أوفيس مصنوعة من خشب البلوط الطبيعي بتشغيل CNC. تصاميم عصرية قابلة للتخصيص في الأبعاد واللون. توفير 40% عن السعر الفردي.",
                ItemName        = "مكتب هوم أوفيس خشب بلوط 120×60 سم",
                IndividualPrice = 250,
                GroupPrice      = 150,
                MinQuantity     = 10,
                CurrentQuantity = 6,
                Status          = CampaignStatus.Active,
                DeadlineDate    = DateTime.UtcNow.AddDays(45),
            },
            new()
            {
                Title           = "حملة منتهية — قطع أكريليك ملون",
                Description     = "حملة منتهية بنجاح. تم التسليم لجميع المشتركين. شكراً لثقتكم.",
                ItemName        = "قطعة أكريليك ملونة مخصصة",
                IndividualPrice = 20,
                GroupPrice      = 12,
                MinQuantity     = 25,
                CurrentQuantity = 25,
                Status          = CampaignStatus.Delivered,
                DeadlineDate    = DateTime.UtcNow.AddDays(-10),
            },
        };

        db.GroupBuyingCampaigns.AddRange(campaigns);
        await db.SaveChangesAsync();

        // المشاركون
        var ali   = await um.FindByEmailAsync("ali@dmp.jo");
        var sara  = await um.FindByEmailAsync("sara@dmp.jo");
        var omar  = await um.FindByEmailAsync("omar@dmp.jo");
        var reem  = await um.FindByEmailAsync("reem@dmp.jo");
        var khaled= await um.FindByEmailAsync("khaled@dmp.jo");

        // حملة 1 — 31 مشارك
        var c1 = campaigns[0];
        foreach (var (user, qty) in new[] { (ali, 8), (sara, 10), (omar, 7), (reem, 6) })
        {
            if (user == null) continue;
            db.CampaignParticipants.Add(new CampaignParticipant { CampaignId = c1.Id, UserId = user.Id, Quantity = qty, JoinedAt = DateTime.UtcNow.AddDays(-5) });
        }

        // حملة 3 — 8 مشاركين
        var c3 = campaigns[2];
        foreach (var (user, qty) in new[] { (ali, 3), (sara, 5) })
        {
            if (user == null) continue;
            db.CampaignParticipants.Add(new CampaignParticipant { CampaignId = c3.Id, UserId = user.Id, Quantity = qty, JoinedAt = DateTime.UtcNow.AddDays(-3) });
        }

        // حملة 4 — 12 مشارك
        var c4 = campaigns[3];
        foreach (var (user, qty) in new[] { (ali, 5), (khaled, 4), (reem, 3) })
        {
            if (user == null) continue;
            db.CampaignParticipants.Add(new CampaignParticipant { CampaignId = c4.Id, UserId = user.Id, Quantity = qty, JoinedAt = DateTime.UtcNow.AddDays(-8) });
        }

        await db.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────
    private static async Task EnsureSuppliers(ApplicationDbContext db)
    {
        if (await db.Suppliers.AnyAsync()) return;

        db.Suppliers.AddRange(new[]
        {
            new Supplier
            {
                Name        = "شركة الأردن للمواد الخام",
                Description = "موردون متخصصون في توريد المواد الخام الصناعية — ألومنيوم، نحاس، فولاذ — لورش التصنيع في مختلف أنحاء الأردن.",
                City        = "عمّان",
                Address     = "منطقة الجاردنز الصناعية، شارع المدينة المنورة",
                Phone       = "+962 6 555 1010",
                Email       = "info@rawmat-jo.com",
                Website     = "https://rawmat-jo.com",
                Materials   = "ألومنيوم، نحاس، فولاذ، ستانلس ستيل",
                IsApproved  = true,
                CreatedAt   = DateTime.UtcNow.AddDays(-90),
            },
            new Supplier
            {
                Name        = "مورد البلاستيك والبوليمرات",
                Description = "توريد خيوط الطباعة ثلاثية الأبعاد وألواح البلاستيك بأنواعها — PLA، ABS، PETG، TPU — بكميات الجملة والتجزئة.",
                City        = "الزرقاء",
                Address     = "المنطقة الصناعية الأولى، شارع ابن خلدون",
                Phone       = "+962 5 388 2200",
                Email       = "sales@plastics-jo.com",
                Website     = null,
                Materials   = "PLA، ABS، PETG، TPU، Nylon، بولي كربونيت",
                IsApproved  = true,
                CreatedAt   = DateTime.UtcNow.AddDays(-75),
            },
            new Supplier
            {
                Name        = "مكتبة الإلكترونيات — الأردن",
                Description = "قطع إلكترونية ولوحات PCB فارغة ومكونات SMD وأدوات اللحام، نخدم ورش تصنيع الإلكترونيات والهواة.",
                City        = "عمّان",
                Address     = "شارع الرينبو، المدينة التقنية",
                Phone       = "+962 6 461 3300",
                Email       = "contact@elec-store.jo",
                Website     = "https://elec-store.jo",
                Materials   = "مقاومات، مكثفات، متحكمات، لوحات Arduino/ESP32، لوحات PCB",
                IsApproved  = true,
                CreatedAt   = DateTime.UtcNow.AddDays(-60),
            },
            new Supplier
            {
                Name        = "مستودع الأخشاب الأردني",
                Description = "توريد ألواح الخشب الطبيعي والـ MDF وخشب الأبلكاش لورش النجارة وقطع الليزر.",
                City        = "إربد",
                Address     = "طريق الشيخ حسين، المنطقة الصناعية",
                Phone       = "+962 2 724 8800",
                Email       = "wood@ard-timber.jo",
                Website     = null,
                Materials   = "خشب بلوط، خشب زان، MDF، أبلكاش، خشب جوز",
                IsApproved  = true,
                CreatedAt   = DateTime.UtcNow.AddDays(-45),
            },
            new Supplier
            {
                Name        = "مورد الأكريليك والزجاج",
                Description = "ألواح الأكريليك الشفافة والملونة بجميع السماكات، مناسبة لقطع الليزر والإشارات التجارية.",
                City        = "عمّان",
                Address     = "شارع الأميرة بسمة، أبو نصير",
                Phone       = "+962 6 514 7700",
                Email       = "acrylic@clearsheets.jo",
                Website     = "https://clearsheets.jo",
                Materials   = "أكريليك شفاف، أكريليك ملون، بلكسي غلاس، ألواح الـ Polycarbonate",
                IsApproved  = true,
                CreatedAt   = DateTime.UtcNow.AddDays(-30),
            },
        });

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
}
