using DMP.Web.Data;
using DMP.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DMP.Web.Tests;

public class FileServiceTests
{
    private sealed class FakeFormFile : IFormFile
    {
        private readonly byte[] _content;
        public FakeFormFile(string fileName, string contentType, byte[]? content = null)
        {
            FileName = fileName;
            ContentType = contentType;
            _content = content ?? new byte[] { 1, 2, 3 };
        }
        public string ContentType { get; }
        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length => _content.Length;
        public string Name => "file";
        public string FileName { get; }
        public void CopyTo(Stream target) => target.Write(_content, 0, _content.Length);
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            target.Write(_content, 0, _content.Length);
            return Task.CompletedTask;
        }
        public Stream OpenReadStream() => new MemoryStream(_content);
    }

    private static byte[] MakeBytes(long size)
    {
        var b = new byte[size];
        for (long i = 0; i < size; i++) b[i] = (byte)(i % 251);
        return b;
    }

    [Fact]
    public async Task SaveImageAsync_rejects_oversized_image()
    {
        var db = TestDb.Create();
        var svc = new FileService(db);
        var tooBig = new FakeFormFile("big.jpg", "image/jpeg", MakeBytes(5 * 1024 * 1024 + 1));

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SaveImageAsync(tooBig, "products"));
    }

    [Fact]
    public async Task SaveImageAsync_rejects_unsupported_extension()
    {
        var db = TestDb.Create();
        var svc = new FileService(db);
        var bad = new FakeFormFile("virus.exe", "application/octet-stream");

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.SaveImageAsync(bad, "products"));
    }

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.png")]
    [InlineData("photo.webp")]
    public async Task SaveImageAsync_accepts_supported_image_extensions(string name)
    {
        var db = TestDb.Create();
        var svc = new FileService(db);
        var file = new FakeFormFile(name, "image/jpeg", MakeBytes(100));

        var url = await svc.SaveImageAsync(file, "products");

        Assert.NotNull(url);
        Assert.StartsWith("/files/", url);
        Assert.Equal(1, await db.StoredFiles.CountAsync());
    }

    [Fact]
    public async Task SaveImagesAsync_skips_empty_files()
    {
        var db = TestDb.Create();
        var svc = new FileService(db);
        var empty = new FakeFormFile("empty.jpg", "image/jpeg", Array.Empty<byte>());
        var good = new FakeFormFile("good.jpg", "image/jpeg", MakeBytes(10));

        var urls = await svc.SaveImagesAsync(new[] { empty, good }, "products");

        Assert.Single(urls);
        Assert.Equal(1, await db.StoredFiles.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_ignores_non_files_url()
    {
        var db = TestDb.Create();
        var svc = new FileService(db);

        await svc.DeleteAsync("/uploads/somewhere.png");

        Assert.Equal(0, await db.StoredFiles.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_removes_matching_stored_file()
    {
        var db = TestDb.Create();
        var svc = new FileService(db);
        var file = new FakeFormFile("photo.jpg", "image/jpeg", MakeBytes(10));

        var url = await svc.SaveImageAsync(file, "covers");

        await svc.DeleteAsync(url);

        Assert.Equal(0, await db.StoredFiles.CountAsync());
    }

    [Fact]
    public async Task EmptyOrNull_file_returns_null()
    {
        var db = TestDb.Create();
        var svc = new FileService(db);

        Assert.Null(await svc.SaveImageAsync(null, "products"));
        var empty = new FakeFormFile("empty.jpg", "image/jpeg", Array.Empty<byte>());
        Assert.Null(await svc.SaveImageAsync(empty, "products"));
    }
}
