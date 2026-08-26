namespace DMP.Web.Models;

public enum MachineCategory
{
    Printing3D  = 1,
    LaserCutting = 2,
    CNC          = 3,
    PCB          = 4,
    Woodwork     = 5,
    MetalWork    = 6,
    Acrylic      = 7,
    Other        = 8
}

public enum DeliveryOption
{
    PickupOnly   = 1,
    LocalDelivery = 2,
    Nationwide   = 3
}

public static class EnumHelpers
{
    private static bool IsEnglish =>
        System.Globalization.CultureInfo.CurrentUICulture.Name
            .StartsWith("en", StringComparison.OrdinalIgnoreCase);

    public static string ToArabic(this MachineCategory c) =>
        IsEnglish ? c.ToEnglish() : c switch
        {
            MachineCategory.Printing3D   => "طباعة ثلاثية الأبعاد",
            MachineCategory.LaserCutting => "قطع بالليزر",
            MachineCategory.CNC          => "CNC",
            MachineCategory.PCB          => "PCB",
            MachineCategory.Woodwork     => "أعمال خشبية",
            MachineCategory.MetalWork    => "أعمال معدنية",
            MachineCategory.Acrylic      => "أكريليك",
            MachineCategory.Other        => "أخرى",
            _ => c.ToString()
        };

    public static string ToEnglish(this MachineCategory c) => c switch
    {
        MachineCategory.Printing3D   => "3D Printing",
        MachineCategory.LaserCutting => "Laser Cutting",
        MachineCategory.CNC          => "CNC",
        MachineCategory.PCB          => "PCB",
        MachineCategory.Woodwork     => "Woodwork",
        MachineCategory.MetalWork    => "Metal Work",
        MachineCategory.Acrylic      => "Acrylic",
        MachineCategory.Other        => "Other",
        _ => c.ToString()
    };

    public static string ToDisplay(this MachineCategory c) =>
        IsEnglish ? c.ToEnglish() : c.ToArabic();

    public static string ToArabic(this DeliveryOption d) =>
        IsEnglish ? d.ToEnglish() : d switch
        {
            DeliveryOption.PickupOnly    => "استلام من الورشة فقط",
            DeliveryOption.LocalDelivery => "توصيل محلي",
            DeliveryOption.Nationwide    => "توصيل لجميع المناطق",
            _ => d.ToString()
        };

    public static string ToEnglish(this DeliveryOption d) => d switch
    {
        DeliveryOption.PickupOnly    => "Pickup only",
        DeliveryOption.LocalDelivery => "Local delivery",
        DeliveryOption.Nationwide    => "Nationwide delivery",
        _ => d.ToString()
    };

    public static string ToDisplay(this DeliveryOption d) =>
        IsEnglish ? d.ToEnglish() : d.ToArabic();

    public static string ToArabic(this ProductCategory c) =>
        IsEnglish ? c.ToEnglish() : c switch
        {
            ProductCategory.CNC          => "CNC",
            ProductCategory.Printing3D   => "طباعة ثلاثية الأبعاد",
            ProductCategory.LaserCutting  => "قطع بالليزر",
            ProductCategory.Electronics  => "إلكترونيات",
            ProductCategory.Woodwork     => "أعمال خشبية",
            ProductCategory.MetalWork    => "أعمال معدنية",
            ProductCategory.Acrylic      => "أكريليك",
            ProductCategory.Accessories  => "ملحقات",
            ProductCategory.RawMaterials => "خامات ومواد",
            ProductCategory.Other        => "أخرى",
            _ => c.ToString()
        };

    public static string ToEnglish(this ProductCategory c) => c switch
    {
        ProductCategory.CNC          => "CNC",
        ProductCategory.Printing3D   => "3D Printing",
        ProductCategory.LaserCutting  => "Laser Cutting",
        ProductCategory.Electronics  => "Electronics",
        ProductCategory.Woodwork     => "Woodwork",
        ProductCategory.MetalWork    => "Metal Work",
        ProductCategory.Acrylic      => "Acrylic",
        ProductCategory.Accessories  => "Accessories",
        ProductCategory.RawMaterials => "Raw Materials",
        ProductCategory.Other        => "Other",
        _ => c.ToString()
    };

    public static string ToDisplay(this ProductCategory c) =>
        IsEnglish ? c.ToEnglish() : c.ToArabic();

    public static string ToArabic(this SellerType s) =>
        IsEnglish ? s.ToEnglish() : s switch
        {
            SellerType.Admin        => "الإدارة",
            SellerType.Manufacturer => "مصنّع",
            _ => s.ToString()
        };

    public static string ToEnglish(this SellerType s) => s switch
    {
        SellerType.Admin        => "Admin",
        SellerType.Manufacturer => "Manufacturer",
        _ => s.ToString()
    };

    public static string ToDisplay(this SellerType s) =>
        IsEnglish ? s.ToEnglish() : s.ToArabic();
}
