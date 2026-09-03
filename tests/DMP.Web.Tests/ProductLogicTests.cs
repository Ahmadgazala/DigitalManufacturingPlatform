using DMP.Web.Models;

namespace DMP.Web.Tests;

public class ProductLogicTests
{
    [Fact]
    public void CoverImage_prefers_explicit_cover_when_present()
    {
        var product = new Product
        {
            ImagePath = "/uploads/lone.png",
            Images = new List<ProductImage>
            {
                new() { ImagePath = "/uploads/second.png", IsCover = false },
                new() { ImagePath = "/uploads/cover.png", IsCover = true }
            }
        };

        Assert.Equal("/uploads/cover.png", product.CoverImage);
    }

    [Fact]
    public void CoverImage_falls_back_to_first_image_when_no_cover_marked()
    {
        var product = new Product
        {
            ImagePath = "/uploads/lone.png",
            Images = new List<ProductImage>
            {
                new() { ImagePath = "/uploads/first.png", IsCover = false },
                new() { ImagePath = "/uploads/second.png", IsCover = false }
            }
        };

        Assert.Equal("/uploads/first.png", product.CoverImage);
    }

    [Fact]
    public void CoverImage_falls_back_to_ImagePath_when_no_images()
    {
        var product = new Product { ImagePath = "/uploads/lone.png", Images = new List<ProductImage>() };
        Assert.Equal("/uploads/lone.png", product.CoverImage);
    }

    [Fact]
    public void CoverImage_null_when_no_images_and_no_legacy_path()
    {
        var product = new Product { ImagePath = null, Images = new List<ProductImage>() };
        Assert.Null(product.CoverImage);
    }

    [Fact]
    public void AverageRating_is_zero_when_no_reviews()
    {
        var product = new Product { Reviews = new List<ProductReview>() };
        Assert.Equal(0, product.AverageRating);
        Assert.Equal(0, product.ReviewsCount);
    }

    [Fact]
    public void AverageRating_rounds_to_one_decimal()
    {
        var product = new Product
        {
            Reviews = new List<ProductReview>
            {
                new() { Rating = 5 },
                new() { Rating = 4 },
                new() { Rating = 3 }
            }
        };

        // (5+4+3)/3 = 4.0
        Assert.Equal(4.0, product.AverageRating);
        Assert.Equal(3, product.ReviewsCount);
    }

    [Fact]
    public void AverageRating_averages_ratings_correctly()
    {
        var product = new Product
        {
            Reviews = new List<ProductReview>
            {
                new() { Rating = 5 },
                new() { Rating = 4 }
            }
        };

        Assert.Equal(4.5, product.AverageRating);
    }
}
