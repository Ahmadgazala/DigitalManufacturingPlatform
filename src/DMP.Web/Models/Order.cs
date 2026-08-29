using System.ComponentModel.DataAnnotations;

namespace DMP.Web.Models;

public enum OrderStatus
{
    Pending     = 0,
    UnderReview = 1,
    Paid        = 2,
    Cancelled   = 3
}

public class Order
{
    public int Id { get; set; }

    [MaxLength(32)]
    public string OrderNumber { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CustomerEmail { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ContactPhone { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "العنوان طويل جداً")]
    public string? ShippingAddress { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public decimal TotalAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [MaxLength(200)]
    public string? PaymentReceiptPath { get; set; }

    [MaxLength(300)]
    public string? PaymentReviewNote { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? PaidAt { get; set; }

    public ApplicationUser? Customer { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ImagePath { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; }
}

public static class OrderStatusHelpers
{
    private static bool IsEnglish =>
        System.Globalization.CultureInfo.CurrentUICulture.Name
            .StartsWith("en", StringComparison.OrdinalIgnoreCase);

    public static string ToDisplay(this OrderStatus s)
    {
        if (IsEnglish) return s.ToEnglish();
        return s switch
        {
            OrderStatus.Pending     => "بانتظار الدفع",
            OrderStatus.UnderReview => "بانتظار مراجعة الإيصال",
            OrderStatus.Paid        => "تم الدفع",
            OrderStatus.Cancelled   => "ملغي",
            _ => s.ToString()
        };
    }

    public static string ToEnglish(this OrderStatus s) => s switch
    {
        OrderStatus.Pending     => "Awaiting payment",
        OrderStatus.UnderReview => "Receipt under review",
        OrderStatus.Paid        => "Paid",
        OrderStatus.Cancelled   => "Cancelled",
        _ => s.ToString()
    };
}