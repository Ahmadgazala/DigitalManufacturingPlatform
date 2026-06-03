using System.ComponentModel.DataAnnotations;

namespace DMP.Web.Models;

public enum CampaignStatus
{
    Active          = 1,
    AwaitingPayment = 2,  // اكتمل النصاب — بانتظار دفع المشاركين
    Confirmed       = 3,
    Shipping        = 4,
    Delivered       = 5,
    Cancelled       = 6
}

public static class CampaignStatusHelpers
{
    public static string ToArabic(this CampaignStatus s) => s switch
    {
        CampaignStatus.Active          => "نشطة",
        CampaignStatus.AwaitingPayment => "بانتظار الدفع",
        CampaignStatus.Confirmed       => "مؤكدة",
        CampaignStatus.Shipping        => "في الشحن",
        CampaignStatus.Delivered       => "تم التوصيل",
        CampaignStatus.Cancelled       => "ملغية",
        _ => s.ToString()
    };

    public static string StatusColor(this CampaignStatus s) => s switch
    {
        CampaignStatus.Active          => "chip-green",
        CampaignStatus.AwaitingPayment => "chip-amber",
        CampaignStatus.Confirmed       => "chip",
        CampaignStatus.Shipping        => "chip",
        CampaignStatus.Delivered       => "chip-steel",
        CampaignStatus.Cancelled       => "chip-red",
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

    /// <summary>الحد الأدنى للطلب من كل مصنّع (0 = غير محدد)</summary>
    [Range(0, 10000)]
    public int MinOrderPerManufacturer { get; set; } = 0;

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

    // --- الدفع ---
    public ParticipantPaymentStatus PaymentStatus { get; set; } = ParticipantPaymentStatus.NotRequested;
    public string?   StripeSessionId { get; set; }
    public decimal?  PaidAmount      { get; set; }
    public DateTime? PaidAt          { get; set; }
}

public enum ParticipantPaymentStatus
{
    NotRequested = 0, // لم يُطلب الدفع بعد
    Pending      = 1, // طُلب الدفع — بانتظار المصنّع
    Paid         = 2, // دفع بنجاح
    Refunded     = 3  // مُسترجع
}
