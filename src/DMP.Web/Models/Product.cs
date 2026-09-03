using System.ComponentModel.DataAnnotations;

namespace DMP.Web.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم المنتج مطلوب")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "السعر مطلوب")]
    [Range(0.01, 999999.99, ErrorMessage = "السعر غير صحيح")]
    public decimal Price { get; set; }

    [StringLength(200)]
    public string? ImagePath { get; set; }

    public ProductCategory Category { get; set; } = ProductCategory.Other;

    public SellerType SellerType { get; set; }

    public string? SellerUserId { get; set; }

    public int? ManufacturerId { get; set; }

    public bool IsActive { get; set; } = true;

    public int Stock { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? SellerUser { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    public List<ProductReview> Reviews { get; set; } = new();
    public int ReviewsCount => Reviews.Count;
    public double AverageRating => Reviews.Count == 0 ? 0 : Math.Round(Reviews.Average(r => r.Rating), 1);

    public List<ProductImage> Images { get; set; } = new();

    public string? CoverImage =>
        Images.FirstOrDefault(i => i.IsCover)?.ImagePath
        ?? Images.FirstOrDefault()?.ImagePath
        ?? ImagePath;
}

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [StringLength(200)]
    public string ImagePath { get; set; } = string.Empty;

    public bool IsCover { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ProductCategory
{
    CNC           = 1,
    Printing3D    = 2,
    LaserCutting  = 3,
    Electronics   = 4,
    Woodwork      = 5,
    MetalWork     = 6,
    Acrylic       = 7,
    Accessories   = 8,
    RawMaterials  = 9,
    Other         = 10
}

public enum SellerType
{
    Admin        = 1,
    Manufacturer = 2
}
