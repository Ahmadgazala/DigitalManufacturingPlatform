using DMP.Web.Models;

namespace DMP.Web.Tests;

public class OrderStatusTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, "Awaiting payment")]
    [InlineData(OrderStatus.UnderReview, "Receipt under review")]
    [InlineData(OrderStatus.Paid, "Paid")]
    [InlineData(OrderStatus.Processing, "Processing")]
    [InlineData(OrderStatus.Cancelled, "Cancelled")]
    public void OrderStatus_ToEnglish_returns_expected_labels(OrderStatus status, string expected)
    {
        Assert.Equal(expected, status.ToEnglish());
    }

    [Fact]
    public void Order_IsCashOnDelivery_true_when_method_is_cash()
    {
        var order = new Order { PaymentMethod = PaymentMethod.CashOnDelivery };
        Assert.True(order.IsCashOnDelivery);
    }

    [Fact]
    public void Order_IsCashOnDelivery_false_when_method_is_cliq()
    {
        var order = new Order { PaymentMethod = PaymentMethod.CliQ };
        Assert.False(order.IsCashOnDelivery);
    }

    [Fact]
    public void PaymentMethod_ToEnglish_cod_returns_cash_on_delivery()
    {
        Assert.Equal("Cash on delivery", PaymentMethod.CashOnDelivery.ToEnglish());
    }

    [Fact]
    public void PaymentMethod_ToEnglish_cliq_returns_pay_via_cliq()
    {
        Assert.Equal("Pay via CliQ", PaymentMethod.CliQ.ToEnglish());
    }

    [Fact]
    public void OrderStatus_defaults_to_pending_on_new_order()
    {
        var order = new Order();
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(PaymentMethod.CliQ, order.PaymentMethod);
    }
}
