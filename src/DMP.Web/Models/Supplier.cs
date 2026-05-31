using System.ComponentModel.DataAnnotations;

namespace DMP.Web.Models;

public class Supplier
{
    public int Id { get; set; }

    [Required(ErrorMessage = "اسم المورد مطلوب")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(300)]
    public string? Website { get; set; }

    [StringLength(500)]
    public string? Materials { get; set; }

    public string? LogoPath { get; set; }

    public bool IsApproved { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
