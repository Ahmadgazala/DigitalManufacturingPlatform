using DMP.Web.Models;

namespace DMP.Web.Tests;

public class ManufacturerLogicTests
{
    [Fact]
    public void NewManufacturer_is_active_by_default()
    {
        var manufacturer = new Manufacturer();
        Assert.True(manufacturer.IsActive);
    }

    [Fact]
    public void NewManufacturer_is_not_approved_by_default()
    {
        var manufacturer = new Manufacturer();
        Assert.False(manufacturer.IsApproved);
    }

    [Fact]
    public void Toggling_IsActive_hides_from_public_visibility()
    {
        var manufacturer = new Manufacturer { IsApproved = true, IsActive = true };
        bool IsPubliclyVisible() => manufacturer.IsApproved && manufacturer.IsActive;

        Assert.True(IsPubliclyVisible());

        manufacturer.IsActive = false;
        Assert.False(IsPubliclyVisible());
    }
}
