namespace UnderTest;

/// <summary>
/// A synthetic checkout order for a fictional storefront. This is the "system
/// under test" that the 48 seeded requirements describe. Nothing here talks to
/// a real payment processor, carrier, or tax service; every field is plain data
/// and every rule in <see cref="OrderRules"/> is a pure function of it.
/// </summary>
public sealed record Order
{
    public string OrderId { get; init; } = "ORD-1001";
    public string CustomerEmail { get; init; } = "jane.doe@example.com";
    public int CustomerAge { get; init; } = 34;

    public string ShippingStreet { get; init; } = "123 Main St";
    public string ShippingCity { get; init; } = "Springfield";
    public string ShippingState { get; init; } = "IL";
    public string ShippingZip { get; init; } = "62704";
    public string ShippingCountry { get; init; } = "US";

    public string PaymentMethod { get; init; } = "CreditCard";
    public string CardNumber { get; init; } = "4111111111111111";
    public int CardExpiryMonth { get; init; } = 12;
    public int CardExpiryYear { get; init; } = 2027;
    public string Cvv { get; init; } = "123";

    public string DiscountCode { get; init; } = "";
    public decimal DiscountPercent { get; init; } = 0m;
    public int PromoStackCount { get; init; } = 0;
    public int MaxPromoStack { get; init; } = 1;

    public int ItemCount { get; init; } = 2;
    public decimal ItemUnitPrice { get; init; } = 25.00m;
    public decimal TotalAmount { get; init; } = 50.00m;
    public decimal TaxAmount { get; init; } = 4.00m;
    public string Currency { get; init; } = "USD";

    public int InventoryAvailable { get; init; } = 10;
    public bool IsBackorder { get; init; } = false;
    public string WarehouseZone { get; init; } = "A1";

    public bool IsFraudFlagged { get; init; } = false;
    public string CustomerLoyaltyTier { get; init; } = "Silver";

    public bool IsGift { get; init; } = false;
    public string GiftMessage { get; init; } = "";

    public decimal WeightKg { get; init; } = 1.5m;
    public string ShippingMethod { get; init; } = "Standard";
    public bool IsExpressShipping { get; init; } = false;
    public bool IsInternational { get; init; } = false;
    public bool HasInsurance { get; init; } = false;
    public bool IsHazardous { get; init; } = false;
    public bool RequiresSignature { get; init; } = false;

    public bool RefundRequested { get; init; } = false;
    public decimal RefundAmount { get; init; } = 0m;

    public bool IsSubscription { get; init; } = false;
    public int SubscriptionIntervalDays { get; init; } = 0;

    public bool TaxExempt { get; init; } = false;
    public string TaxExemptionId { get; init; } = "";

    public bool IsB2B { get; init; } = false;
    public string PurchaseOrderNumber { get; init; } = "";
    public decimal CreditLimit { get; init; } = 1000.00m;
    public decimal OutstandingBalance { get; init; } = 0.00m;

    public bool IsPriority { get; init; } = false;
    public int SlaHours { get; init; } = 72;

    public string CarrierId { get; init; } = "UPS";
    public string TrackingNumber { get; init; } = "1Z9999999999999999";
    public bool LabelPrinted { get; init; } = false;

    /// <summary>
    /// The single canonical order that passes all 48 rules simultaneously.
    /// Every "bad" fixture in the test suite is this order with exactly the
    /// one field a given rule cares about mutated with a C# `with` expression,
    /// so a failing rule test can never be explained by an unrelated field.
    /// </summary>
    public static Order Good() => new();
}
