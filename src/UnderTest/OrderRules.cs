using System.Linq;

namespace UnderTest;

/// <summary>
/// The 48 checkout rules that back REQ-001 through REQ-048 in
/// data/requirements.csv. Each method is a pure, independent predicate over
/// one Order. Grouped into eight six-rule categories matching the
/// requirements manifest: payment, shipping address, discount and pricing,
/// inventory and backorder, fraud and risk, tax and compliance, subscription
/// and loyalty, shipping method and carrier.
/// </summary>
public static class OrderRules
{
    private static bool AllDigits(string s) => s.Length > 0 && s.All(char.IsDigit);

    // ---- Payment (REQ-001..006) ----

    public static bool Req001(Order o) => o.CardNumber.Length == 16 && AllDigits(o.CardNumber);

    public static bool Req002(Order o) => o.CardExpiryYear >= 2026;

    public static bool Req003(Order o) => o.Cvv.Length == 3 && AllDigits(o.Cvv);

    private static readonly string[] SupportedPaymentMethods = { "CreditCard", "PayPal", "GiftCard", "Invoice" };
    public static bool Req004(Order o) => SupportedPaymentMethods.Contains(o.PaymentMethod);

    public static bool Req005(Order o) => o.PaymentMethod != "Invoice" || o.OutstandingBalance <= o.CreditLimit;

    public static bool Req006(Order o) => o.TotalAmount > 0m;

    // ---- Shipping address (REQ-007..012) ----

    public static bool Req007(Order o) => !string.IsNullOrWhiteSpace(o.ShippingStreet);

    public static bool Req008(Order o) => !string.IsNullOrWhiteSpace(o.ShippingCity);

    public static bool Req009(Order o) => o.ShippingCountry != "US" || (o.ShippingZip.Length == 5 && AllDigits(o.ShippingZip));

    public static bool Req010(Order o) => o.ShippingCountry != "US" || (o.ShippingState.Length == 2 && o.ShippingState == o.ShippingState.ToUpperInvariant());

    public static bool Req011(Order o) => !string.IsNullOrWhiteSpace(o.ShippingCountry) && o.ShippingCountry.Length <= 3;

    public static bool Req012(Order o) => !o.IsInternational || o.TotalAmount <= 500m || o.HasInsurance;

    // ---- Discount and pricing (REQ-013..018) ----

    public static bool Req013(Order o) => o.DiscountPercent >= 0m && o.DiscountPercent <= 75m;

    public static bool Req014(Order o) => o.DiscountPercent == 0m || !string.IsNullOrWhiteSpace(o.DiscountCode);

    public static bool Req015(Order o) => o.PromoStackCount <= o.MaxPromoStack;

    public static bool Req016(Order o)
    {
        var expected = o.ItemCount * o.ItemUnitPrice * (1m - o.DiscountPercent / 100m);
        return System.Math.Abs(o.TotalAmount - expected) < 0.01m;
    }

    public static bool Req017(Order o) => o.TaxAmount >= 0m;

    public static bool Req018(Order o) => !o.TaxExempt || !string.IsNullOrWhiteSpace(o.TaxExemptionId);

    // ---- Inventory and backorder (REQ-019..024) ----

    public static bool Req019(Order o) => o.ItemCount >= 1;

    public static bool Req020(Order o) => o.InventoryAvailable >= 0;

    public static bool Req021(Order o) => o.IsBackorder || o.InventoryAvailable >= o.ItemCount;

    public static bool Req022(Order o) => !o.IsBackorder || o.WarehouseZone == "BACKORDER";

    public static bool Req023(Order o) => !o.IsHazardous || !o.IsExpressShipping;

    private static readonly string[] KnownZones = { "A1", "A2", "B1", "B2", "BACKORDER" };
    public static bool Req024(Order o) => KnownZones.Contains(o.WarehouseZone);

    // ---- Fraud and risk (REQ-025..030) ----

    public static bool Req025(Order o) => !o.IsFraudFlagged || !o.IsPriority;

    public static bool Req026(Order o) => o.CustomerEmail.Contains('@') && o.CustomerEmail.Contains('.');

    public static bool Req027(Order o) => !o.IsHazardous || o.CustomerAge >= 21;

    public static bool Req028(Order o) => o.RefundAmount <= o.TotalAmount;

    public static bool Req029(Order o) => o.RefundAmount == 0m || o.RefundRequested;

    public static bool Req030(Order o) => !o.IsGift || (o.GiftMessage.Length > 0 && o.GiftMessage.Length <= 200);

    // ---- Tax and compliance (REQ-031..036) ----

    public static bool Req031(Order o) => !o.IsB2B || !string.IsNullOrWhiteSpace(o.PurchaseOrderNumber);

    private static readonly string[] SupportedCurrencies = { "USD", "EUR", "GBP", "CAD" };
    public static bool Req032(Order o) => SupportedCurrencies.Contains(o.Currency);

    public static bool Req033(Order o) => o.TaxAmount >= 0m && o.TaxAmount <= o.TotalAmount * 0.30m;

    public static bool Req034(Order o) => !o.IsInternational || (o.ShippingCountry.Length >= 2 && o.ShippingCountry.Length <= 3);

    public static bool Req035(Order o) => !o.RequiresSignature || o.SlaHours >= 24;

    public static bool Req036(Order o) => !o.IsPriority || o.SlaHours <= 48;

    // ---- Subscription and loyalty (REQ-037..042) ----

    public static bool Req037(Order o) => !o.IsSubscription || o.SubscriptionIntervalDays > 0;

    private static readonly string[] KnownTiers = { "None", "Silver", "Gold", "Platinum" };
    public static bool Req038(Order o) => KnownTiers.Contains(o.CustomerLoyaltyTier);

    public static bool Req039(Order o) => o.CustomerLoyaltyTier != "Platinum" || !o.IsFraudFlagged;

    public static bool Req040(Order o) => !o.IsSubscription || o.SubscriptionIntervalDays >= 7;

    public static bool Req041(Order o) => o.MaxPromoStack >= 1;

    public static bool Req042(Order o) => o.CustomerLoyaltyTier != "None" || o.DiscountPercent <= 10m;

    // ---- Shipping method and carrier (REQ-043..048) ----

    private static readonly string[] SupportedShippingMethods = { "Standard", "Express", "Overnight" };
    public static bool Req043(Order o) => SupportedShippingMethods.Contains(o.ShippingMethod);

    public static bool Req044(Order o) => o.IsExpressShipping == (o.ShippingMethod == "Express" || o.ShippingMethod == "Overnight");

    private static readonly string[] KnownCarriers = { "UPS", "FedEx", "USPS", "DHL" };
    public static bool Req045(Order o) => KnownCarriers.Contains(o.CarrierId);

    public static bool Req046(Order o) => !o.LabelPrinted || !string.IsNullOrWhiteSpace(o.TrackingNumber);

    public static bool Req047(Order o) => o.WeightKg > 0m;

    public static bool Req048(Order o) => o.WeightKg <= 30m || o.ShippingMethod != "Standard";
}
