using System.ComponentModel.DataAnnotations;

namespace DMP.Web.Models;

public class Manufacturer
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required(ErrorMessage = "اسم الورشة مطلوب")]
    [StringLength(200)]
    public string WorkshopName { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Bio { get; set; }

    [Required(ErrorMessage = "المدينة مطلوبة")]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Address { get; set; }

    public double? Latitude  { get; set; }
    public double? Longitude { get; set; }

    [Required(ErrorMessage = "رقم التواصل مطلوب")]
    [StringLength(30)]
    public string ContactPhone { get; set; } = string.Empty;

    [StringLength(200)]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    [StringLength(200)]
    public string? WorkingHours { get; set; }

    public DeliveryOption Delivery { get; set; } = DeliveryOption.PickupOnly;

    [StringLength(300)]
    public string? ProductionCapacity { get; set; }

    public string? ProfilePhotoPath { get; set; }
    public string? CoverPhotoPath   { get; set; }

    public bool IsApproved { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Machine>        Machines        { get; set; } = new();
    public List<WorkshopPhoto>  WorkshopPhotos  { get; set; } = new();
    public List<Review>         Reviews         { get; set; } = new();
    public List<PortfolioItem>  PortfolioItems  { get; set; } = new();

    // خصائص محسوبة
    public int    ReviewsCount   => Reviews.Count;
    public double AverageRating  => Reviews.Count == 0 ? 0 : Math.Round(Reviews.Average(r => r.Rating), 1);
}

public class Machine
{
    public int Id { get; set; }

    public int ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    public MachineCategory Category { get; set; }

    [Required(ErrorMessage = "الاسم/الموديل مطلوب")]
    [StringLength(200)]
    public string ModelName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(300)]
    public string? SupportedMaterials { get; set; }

    [StringLength(300)]
    public string? PricingNote { get; set; }
}

public class WorkshopPhoto
{
    public int Id { get; set; }

    public int ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class PortfolioItem
{
    public int Id { get; set; }

    public int ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    [Required(ErrorMessage = "عنوان المشروع مطلوب")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public string? ImagePath { get; set; }

    public MachineCategory? Category { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
}

public class Review
{
    public int Id { get; set; }

    public int ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    [Required]
    public string CustomerUserId { get; set; } = string.Empty;
    public ApplicationUser? Customer { get; set; }

    [Required]
    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
