using DMP.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace DMP.Web.ViewModels;

public class ManufacturerFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم الورشة مطلوب")]
    public string WorkshopName { get; set; } = string.Empty;

    public string? Bio { get; set; }

    [Required(ErrorMessage = "المدينة مطلوبة")]
    public string City { get; set; } = string.Empty;

    public string? Address { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    [Required(ErrorMessage = "رقم التواصل مطلوب")]
    public string ContactPhone { get; set; } = string.Empty;

    [EmailAddress]
    public string? ContactEmail { get; set; }

    public string? WorkingHours { get; set; }

    public DeliveryOption Delivery { get; set; } = DeliveryOption.PickupOnly;

    public string? ProductionCapacity { get; set; }

    public IFormFile? ProfilePhoto { get; set; }

    public string? ExistingProfilePhotoPath { get; set; }

    public IFormFile? CoverPhoto { get; set; }

    public string? ExistingCoverPhotoPath { get; set; }
}

public class MachineFormViewModel
{
    public int Id { get; set; }

    public int ManufacturerId { get; set; }

    [Required(ErrorMessage = "الفئة مطلوبة")]
    public MachineCategory Category { get; set; }

    [Required(ErrorMessage = "اسم/موديل الماكينة مطلوب")]
    public string ModelName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? SupportedMaterials { get; set; }

    public string? PricingNote { get; set; }
}

public class DirectoryViewModel
{
    public List<Manufacturer> Manufacturers { get; set; } = new();

    public string? SearchTerm { get; set; }

    public MachineCategory? Category { get; set; }

    public string? City { get; set; }

    public List<string> Cities { get; set; } = new();
}

public class ManufacturerDashboardViewModel
{
    public Manufacturer Manufacturer { get; set; } = null!;

    public int OpenRequestsCount { get; set; }

    public int PendingQuotationsCount { get; set; }

    public int AcceptedQuotationsCount { get; set; }

    public int CompletedOrdersCount { get; set; }

    public List<ManufacturingRequest> RecentRequests { get; set; } = new();

    public List<Quotation> RecentQuotations { get; set; } = new();
}

public class LoginViewModel
{
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "الاسم الكامل مطلوب")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [MinLength(6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "كلمتا المرور غير متطابقتين")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
