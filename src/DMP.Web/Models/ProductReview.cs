using System.ComponentModel.DataAnnotations;

namespace DMP.Web.Models;

public class ProductReview
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

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