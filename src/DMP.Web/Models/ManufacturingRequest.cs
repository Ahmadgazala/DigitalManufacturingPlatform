using System.ComponentModel.DataAnnotations;

namespace DMP.Web.Models;

public enum RequestStatus
{
    Open       = 1,
    InProgress = 2,
    Completed  = 3,
    Cancelled  = 4
}

public enum QuotationStatus
{
    Pending  = 1,
    Accepted = 2,
    Rejected = 3
}

public enum TrackingStage
{
    Received        = 1,
    InManufacturing = 2,
    ReadyForPickup  = 3,
    Delivered       = 4
}

public enum PaymentStatus
{
    NotPaid     = 0,
    Pending     = 1,
    Paid        = 2,
    Refunded    = 3,
    UnderReview = 4  // رُفعت صورة الدفع وتنتظر موافقة المدير
}

public static class RequestStatusHelpers
{
    private static bool IsEnglish =>
        System.Globalization.CultureInfo.CurrentUICulture.Name
            .StartsWith("en", StringComparison.OrdinalIgnoreCase);

    public static string ToEnglish(this RequestStatus s) => s switch
    {
        RequestStatus.Open       => "Open",
        RequestStatus.InProgress => "In Progress",
        RequestStatus.Completed  => "Completed",
        RequestStatus.Cancelled  => "Cancelled",
        _ => s.ToString()
    };

    public static string ToArabic(this RequestStatus s) =>
        IsEnglish ? s.ToEnglish() : s switch
        {
            RequestStatus.Open       => "مفتوح",
            RequestStatus.InProgress => "جاري التنفيذ",
            RequestStatus.Completed  => "مكتمل",
            RequestStatus.Cancelled  => "ملغي",
            _ => s.ToString()
        };

    public static string ToDisplay(this RequestStatus s) =>
        IsEnglish ? s.ToString() : s.ToArabic();

    public static string ToEnglish(this QuotationStatus s) => s switch
    {
        QuotationStatus.Pending  => "Awaiting reply",
        QuotationStatus.Accepted => "Accepted",
        QuotationStatus.Rejected => "Rejected",
        _ => s.ToString()
    };

    public static string ToArabic(this QuotationStatus s) =>
        IsEnglish ? s.ToEnglish() : s switch
        {
            QuotationStatus.Pending  => "بانتظار الرد",
            QuotationStatus.Accepted => "مقبول",
            QuotationStatus.Rejected => "مرفوض",
            _ => s.ToString()
        };

    public static string ToDisplay(this QuotationStatus s) =>
        IsEnglish ? s.ToString() : s.ToArabic();

    public static string ToEnglish(this TrackingStage s) => s switch
    {
        TrackingStage.Received        => "Received",
        TrackingStage.InManufacturing => "In Manufacturing",
        TrackingStage.ReadyForPickup  => "Ready For Pickup",
        TrackingStage.Delivered       => "Delivered",
        _ => s.ToString()
    };

    public static string ToArabic(this TrackingStage s) =>
        IsEnglish ? s.ToEnglish() : s switch
        {
            TrackingStage.Received        => "تم الاستلام",
            TrackingStage.InManufacturing => "قيد التصنيع",
            TrackingStage.ReadyForPickup  => "جاهز للاستلام",
            TrackingStage.Delivered       => "تم التسليم",
            _ => s.ToString()
        };

    public static string ToIcon(this TrackingStage s) => s switch
    {
        TrackingStage.Received        => "📥",
        TrackingStage.InManufacturing => "⚙️",
        TrackingStage.ReadyForPickup  => "📦",
        TrackingStage.Delivered       => "✅",
        _ => "🔵"
    };

    public static string ToEnglish(this PaymentStatus s) => s switch
    {
        PaymentStatus.NotPaid     => "Not Paid",
        PaymentStatus.Pending     => "Awaiting Payment",
        PaymentStatus.Paid        => "Paid",
        PaymentStatus.Refunded    => "Refunded",
        PaymentStatus.UnderReview => "Under Review",
        _ => s.ToString()
    };

    public static string ToArabic(this PaymentStatus s) =>
        IsEnglish ? s.ToEnglish() : s switch
        {
            PaymentStatus.NotPaid     => "غير مدفوع",
            PaymentStatus.Pending     => "بانتظار الدفع",
            PaymentStatus.Paid        => "مدفوع",
            PaymentStatus.Refunded    => "مُسترجع",
            PaymentStatus.UnderReview => "قيد المراجعة",
            _ => s.ToString()
        };
}

public class ManufacturingRequest
{
    public int Id { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;
    public ApplicationUser? Customer { get; set; }

    public int? ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    [Required(ErrorMessage = "عنوان الطلب مطلوب")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public MachineCategory Category { get; set; }

    [StringLength(200)]
    public string? Material { get; set; }

    [Range(1, 10000)]
    public int Quantity { get; set; } = 1;

    public decimal? Budget { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Open;

    // تتبع الطلب
    public TrackingStage? TrackingStage { get; set; }

    // الدفع
    public PaymentStatus PaymentStatus    { get; set; } = PaymentStatus.NotPaid;
    public string?       StripeSessionId  { get; set; }  // kept for migration compat
    public decimal?      PaidAmount       { get; set; }
    public DateTime?     PaidAt           { get; set; }
    public string?       PaymentReceiptPath { get; set; } // صورة إيصال الدفع (CliQ)
    public string?       PaymentReviewNote  { get; set; } // ملاحظة المدير

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<RequestFile>  Files        { get; set; } = new();
    public List<Quotation>    Quotations   { get; set; } = new();
    public List<OrderUpdate>  OrderUpdates { get; set; } = new();
}

// سجل تحديثات تتبع الطلب
public class OrderUpdate
{
    public int Id { get; set; }

    public int RequestId { get; set; }
    public ManufacturingRequest? Request { get; set; }

    public TrackingStage Stage { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RequestFile
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public ManufacturingRequest? Request { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    public string OriginalFileName { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }
}

public class Quotation
{
    public int Id { get; set; }

    public int RequestId { get; set; }
    public ManufacturingRequest? Request { get; set; }

    public int ManufacturerId { get; set; }
    public Manufacturer? Manufacturer { get; set; }

    [Required(ErrorMessage = "السعر مطلوب")]
    [Range(0.01, 999999)]
    public decimal Price { get; set; }

    [Range(1, 365)]
    public int EstimatedDays { get; set; } = 3;

    [StringLength(1000)]
    public string? Notes { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
