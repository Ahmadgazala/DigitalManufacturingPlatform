using System.ComponentModel.DataAnnotations;

namespace DMP.Web.Models;

public enum CampaignStatus
{
    Active    = 1,
    Confirmed = 2,
    Shipping  = 3,
    Delivered = 4,
    Cancelled = 5
}

public static class CampaignStatusHelpers
{
    public static string ToArabic(this CampaignStatus s) => s switch
    {
        CampaignStatus.Active    => "نشطة",
        CampaignStatus.Confirmed => "مؤكدة",
        CampaignStatus.Shipping  => "في الشحن",
        CampaignStatus.Delivered => "تم التوصيل",
        CampaignStatus.Cancelled => "ملغية",
        _ => s.ToString()
    };

    public static string StatusColor(this CampaignStatus s) => s switch
    {
        CampaignStatus.Active    => "chip-green",
        CampaignStatus.Confirmed => "chip",
        CampaignStatus.Shipping  => "chip",
        CampaignStatus.Delivered => "chip-steel",
        CampaignStatus.Cancelled => "chip-red",
        _ => "chip-steel"
    };
}

public class GroupBuyingCampaign
{
    public int Id { get; set; }

    [Required(ErrorMessage = "عنوان الحملة مطلوب")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "اسم المنتج مطلوب")]
    [StringLength(200)]
    public string ItemName { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    [Range(0.01, 99999)]
    public decimal IndividualPrice { get; set; }

    [Range(0.01, 99999)]
    public decimal GroupPrice { get; set; }

    [Range(1, 100000)]
    public int MinQuantity { get; set; }

    public int CurrentQuantity { get; set; } = 0;

    public CampaignStatus Status { get; set; } = CampaignStatus.Active;

    public DateTime DeadlineDate { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;

    public List<CampaignParticipant> Participants { get; set; } = new();

    public int     ProgressPercent => MinQuantity == 0 ? 0 : Math.Min(100, CurrentQuantity * 100 / MinQuantity);
    public bool    IsActive        => Status == CampaignStatus.Active && DeadlineDate > DateTime.UtcNow;
    public decimal Savings         => IndividualPrice - GroupPrice;
    public int     SavingsPercent  => IndividualPrice == 0 ? 0 : (int)((Savings / IndividualPrice) * 100);
}

public class CampaignParticipant
{
    public int Id { get; set; }

    public int CampaignId { get; set; }
    public GroupBuyingCampaign? Campaign { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; } = 1;

    [StringLength(500)]
    public string? Preferences { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
