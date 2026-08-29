namespace DMP.Web.Models;

public class StoredFile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string? Folder { get; set; }

    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}