using System.ComponentModel.DataAnnotations;

namespace DMP.Web.Models;

public class CartItem
{
    public int Id { get; set; }

    [MaxLength(450)]
    public string CartKey { get; set; } = string.Empty;

    public int ProductId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Product? Product { get; set; }
}