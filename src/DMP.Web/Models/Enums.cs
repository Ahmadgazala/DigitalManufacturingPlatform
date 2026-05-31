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

    public static string ToArabic(this MachineCategory c) => c switch
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

    public static string ToArabic(this DeliveryOption d) => d switch
    {
        DeliveryOption.PickupOnly    => "استلام من الورشة فقط",
        DeliveryOption.LocalDelivery => "توصيل محلي",
        DeliveryOption.Nationwide    => "توصيل لجميع المناطق",
        _ => d.ToString()
    };
}
