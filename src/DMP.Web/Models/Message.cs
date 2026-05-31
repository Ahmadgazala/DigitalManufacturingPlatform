using System.ComponentModel.DataAnnotations;

namespace DMP.Web.Models;

public class Message
{
    public int Id { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;
    public ApplicationUser? Sender { get; set; }

    [Required]
    public string ReceiverId { get; set; } = string.Empty;
    public ApplicationUser? Receiver { get; set; }

    [Required]
    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
