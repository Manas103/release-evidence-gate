using TraceContracts;
using UnderTest;
using Xunit;

namespace UnderTest.Tests;

/// <summary>
/// One test method per requirement (REQ-001..REQ-048). Each test asserts the
/// rule is true for the single canonical good order and false for that same
/// order with exactly the one field the rule cares about mutated, so a
/// failure can never be blamed on an unrelated field. The [Trace] attribute
/// is the only thing TraceabilityGate reads; it does not know or care that
/// these tests use xUnit's [Fact] underneath.
/// </summary>
public class RuleTests
{
    [Fact]
    [Trace("REQ-001")]
    public void Req001_CardNumberMustBeSixteenDigits()
    {
        Assert.True(OrderRules.Req001(Order.Good()));
        Assert.False(OrderRules.Req001(Order.Good() with { CardNumber = "41111111" }));
    }

    [Fact]
    [Trace("REQ-002")]
    public void Req002_CardExpiryYearMustNotBePast()
    {
        Assert.True(OrderRules.Req002(Order.Good()));
        Assert.False(OrderRules.Req002(Order.Good() with { CardExpiryYear = 2020 }));
    }

    [Fact]
    [Trace("REQ-003")]
    public void Req003_CvvMustBeThreeDigits()
    {
        Assert.True(OrderRules.Req003(Order.Good()));
        Assert.False(OrderRules.Req003(Order.Good() with { Cvv = "12" }));
    }

    [Fact]
    [Trace("REQ-004")]
    public void Req004_PaymentMethodMustBeSupported()
    {
        Assert.True(OrderRules.Req004(Order.Good()));
        Assert.False(OrderRules.Req004(Order.Good() with { PaymentMethod = "Bitcoin" }));
    }

    [Fact]
    [Trace("REQ-005")]
    public void Req005_OutstandingBalanceWithinCreditLimitForInvoice()
    {
        Assert.True(OrderRules.Req005(Order.Good()));
        Assert.False(OrderRules.Req005(Order.Good() with { PaymentMethod = "Invoice", OutstandingBalance = 2000m }));
    }

    [Fact]
    [Trace("REQ-006")]
    public void Req006_TotalAmountMustBePositive()
    {
        Assert.True(OrderRules.Req006(Order.Good()));
        Assert.False(OrderRules.Req006(Order.Good() with { TotalAmount = 0m }));
    }

    [Fact]
    [Trace("REQ-007")]
    public void Req007_ShippingStreetMustNotBeEmpty()
    {
        Assert.True(OrderRules.Req007(Order.Good()));
        Assert.False(OrderRules.Req007(Order.Good() with { ShippingStreet = "" }));
    }

    [Fact]
    [Trace("REQ-008")]
    public void Req008_ShippingCityMustNotBeEmpty()
    {
        Assert.True(OrderRules.Req008(Order.Good()));
        Assert.False(OrderRules.Req008(Order.Good() with { ShippingCity = "" }));
    }

    [Fact]
    [Trace("REQ-009")]
    public void Req009_ShippingZipMustBeFiveDigitsForUs()
    {
        Assert.True(OrderRules.Req009(Order.Good()));
        Assert.False(OrderRules.Req009(Order.Good() with { ShippingZip = "ABCDE" }));
    }

    [Fact]
    [Trace("REQ-010")]
    public void Req010_ShippingStateMustBeTwoLetterCodeForUs()
    {
        Assert.True(OrderRules.Req010(Order.Good()));
        Assert.False(OrderRules.Req010(Order.Good() with { ShippingState = "Illinois" }));
    }

    [Fact]
    [Trace("REQ-011")]
    public void Req011_ShippingCountryMustBeNonEmptyShortCode()
    {
        Assert.True(OrderRules.Req011(Order.Good()));
        Assert.False(OrderRules.Req011(Order.Good() with { ShippingCountry = "" }));
    }

    [Fact]
    [Trace("REQ-012")]
    public void Req012_InternationalHighValueOrdersNeedInsurance()
    {
        Assert.True(OrderRules.Req012(Order.Good()));
        Assert.False(OrderRules.Req012(Order.Good() with { IsInternational = true, TotalAmount = 600m }));
    }

    [Fact]
    [Trace("REQ-013")]
    public void Req013_DiscountPercentMustBeInRange()
    {
        Assert.True(OrderRules.Req013(Order.Good()));
        Assert.False(OrderRules.Req013(Order.Good() with { DiscountPercent = 90m }));
    }

    [Fact]
    [Trace("REQ-014")]
    public void Req014_DiscountCodeRequiredWhenDiscounted()
    {
        Assert.True(OrderRules.Req014(Order.Good()));
        Assert.False(OrderRules.Req014(Order.Good() with { DiscountPercent = 10m }));
    }

    [Fact]
    [Trace("REQ-015")]
    public void Req015_PromoStackWithinMax()
    {
        Assert.True(OrderRules.Req015(Order.Good()));
        Assert.False(OrderRules.Req015(Order.Good() with { PromoStackCount = 3 }));
    }

    [Fact]
    [Trace("REQ-016")]
    public void Req016_TotalAmountMatchesPriceArithmetic()
    {
        Assert.True(OrderRules.Req016(Order.Good()));
        Assert.False(OrderRules.Req016(Order.Good() with { TotalAmount = 999m }));
    }

    [Fact]
    [Trace("REQ-017")]
    public void Req017_TaxAmountMustBeNonNegative()
    {
        Assert.True(OrderRules.Req017(Order.Good()));
        Assert.False(OrderRules.Req017(Order.Good() with { TaxAmount = -1m }));
    }

    [Fact]
    [Trace("REQ-018")]
    public void Req018_TaxExemptOrdersNeedExemptionId()
    {
        Assert.True(OrderRules.Req018(Order.Good()));
        Assert.False(OrderRules.Req018(Order.Good() with { TaxExempt = true }));
    }

    [Fact]
    [Trace("REQ-019")]
    public void Req019_ItemCountMustBeAtLeastOne()
    {
        Assert.True(OrderRules.Req019(Order.Good()));
        Assert.False(OrderRules.Req019(Order.Good() with { ItemCount = 0 }));
    }

    [Fact]
    [Trace("REQ-020")]
    public void Req020_InventoryAvailableMustBeNonNegative()
    {
        Assert.True(OrderRules.Req020(Order.Good()));
        Assert.False(OrderRules.Req020(Order.Good() with { InventoryAvailable = -1 }));
    }

    [Fact]
    [Trace("REQ-021")]
    public void Req021_NonBackorderNeedsSufficientInventory()
    {
        Assert.True(OrderRules.Req021(Order.Good()));
        Assert.False(OrderRules.Req021(Order.Good() with { InventoryAvailable = 1 }));
    }

    [Fact]
    [Trace("REQ-022")]
    public void Req022_BackorderNeedsBackorderZone()
    {
        Assert.True(OrderRules.Req022(Order.Good()));
        Assert.False(OrderRules.Req022(Order.Good() with { IsBackorder = true }));
    }

    [Fact]
    [Trace("REQ-023")]
    public void Req023_HazardousMustNotUseExpressShipping()
    {
        Assert.True(OrderRules.Req023(Order.Good()));
        Assert.False(OrderRules.Req023(Order.Good() with { IsHazardous = true, IsExpressShipping = true }));
    }

    [Fact]
    [Trace("REQ-024")]
    public void Req024_WarehouseZoneMustBeKnown()
    {
        Assert.True(OrderRules.Req024(Order.Good()));
        Assert.False(OrderRules.Req024(Order.Good() with { WarehouseZone = "Z9" }));
    }

    [Fact]
    [Trace("REQ-025")]
    public void Req025_FraudFlaggedMustNotBePriority()
    {
        Assert.True(OrderRules.Req025(Order.Good()));
        Assert.False(OrderRules.Req025(Order.Good() with { IsFraudFlagged = true, IsPriority = true }));
    }

    [Fact]
    [Trace("REQ-026")]
    public void Req026_CustomerEmailMustLookValid()
    {
        Assert.True(OrderRules.Req026(Order.Good()));
        Assert.False(OrderRules.Req026(Order.Good() with { CustomerEmail = "not-an-email" }));
    }

    [Fact]
    [Trace("REQ-027")]
    public void Req027_HazardousOrdersNeedAdultCustomer()
    {
        Assert.True(OrderRules.Req027(Order.Good()));
        Assert.False(OrderRules.Req027(Order.Good() with { IsHazardous = true, CustomerAge = 16 }));
    }

    [Fact]
    [Trace("REQ-028")]
    public void Req028_RefundAmountMustNotExceedTotal()
    {
        Assert.True(OrderRules.Req028(Order.Good()));
        Assert.False(OrderRules.Req028(Order.Good() with { RefundAmount = 999m }));
    }

    [Fact]
    [Trace("REQ-029")]
    public void Req029_RefundRequestedFlagMustMatchAmount()
    {
        Assert.True(OrderRules.Req029(Order.Good()));
        Assert.False(OrderRules.Req029(Order.Good() with { RefundAmount = 10m }));
    }

    [Fact]
    [Trace("REQ-030")]
    public void Req030_GiftOrdersNeedGiftMessage()
    {
        Assert.True(OrderRules.Req030(Order.Good()));
        Assert.False(OrderRules.Req030(Order.Good() with { IsGift = true }));
    }

    [Fact]
    [Trace("REQ-031")]
    public void Req031_B2BOrdersNeedPurchaseOrderNumber()
    {
        Assert.True(OrderRules.Req031(Order.Good()));
        Assert.False(OrderRules.Req031(Order.Good() with { IsB2B = true }));
    }

    [Fact]
    [Trace("REQ-032")]
    public void Req032_CurrencyMustBeSupported()
    {
        Assert.True(OrderRules.Req032(Order.Good()));
        Assert.False(OrderRules.Req032(Order.Good() with { Currency = "XYZ" }));
    }

    [Fact]
    [Trace("REQ-033")]
    public void Req033_TaxAmountWithinThirtyPercentOfTotal()
    {
        Assert.True(OrderRules.Req033(Order.Good()));
        Assert.False(OrderRules.Req033(Order.Good() with { TaxAmount = 40m }));
    }

    [Fact]
    [Trace("REQ-034")]
    public void Req034_InternationalOrdersNeedShortCountryCode()
    {
        Assert.True(OrderRules.Req034(Order.Good()));
        Assert.False(OrderRules.Req034(Order.Good() with { IsInternational = true, ShippingCountry = "U" }));
    }

    [Fact]
    [Trace("REQ-035")]
    public void Req035_SignatureRequiredNeedsLongerSla()
    {
        Assert.True(OrderRules.Req035(Order.Good()));
        Assert.False(OrderRules.Req035(Order.Good() with { RequiresSignature = true, SlaHours = 12 }));
    }

    [Fact]
    [Trace("REQ-036")]
    public void Req036_PriorityOrdersNeedShorterSla()
    {
        Assert.True(OrderRules.Req036(Order.Good()));
        Assert.False(OrderRules.Req036(Order.Good() with { IsPriority = true }));
    }

    [Fact]
    [Trace("REQ-037")]
    public void Req037_SubscriptionNeedsPositiveInterval()
    {
        Assert.True(OrderRules.Req037(Order.Good()));
        Assert.False(OrderRules.Req037(Order.Good() with { IsSubscription = true }));
    }

    [Fact]
    [Trace("REQ-038")]
    public void Req038_LoyaltyTierMustBeKnown()
    {
        Assert.True(OrderRules.Req038(Order.Good()));
        Assert.False(OrderRules.Req038(Order.Good() with { CustomerLoyaltyTier = "Diamond" }));
    }

    [Fact]
    [Trace("REQ-039")]
    public void Req039_PlatinumTierMustNotBeFraudFlagged()
    {
        Assert.True(OrderRules.Req039(Order.Good()));
        Assert.False(OrderRules.Req039(Order.Good() with { CustomerLoyaltyTier = "Platinum", IsFraudFlagged = true }));
    }

    [Fact]
    [Trace("REQ-040")]
    public void Req040_SubscriptionIntervalMustBeAtLeastSevenDays()
    {
        Assert.True(OrderRules.Req040(Order.Good()));
        Assert.False(OrderRules.Req040(Order.Good() with { IsSubscription = true, SubscriptionIntervalDays = 3 }));
    }

    [Fact]
    [Trace("REQ-041")]
    public void Req041_MaxPromoStackMustBeAtLeastOne()
    {
        Assert.True(OrderRules.Req041(Order.Good()));
        Assert.False(OrderRules.Req041(Order.Good() with { MaxPromoStack = 0 }));
    }

    [Fact]
    [Trace("REQ-042")]
    public void Req042_NoneTierCappedAtTenPercentDiscount()
    {
        Assert.True(OrderRules.Req042(Order.Good()));
        Assert.False(OrderRules.Req042(Order.Good() with { CustomerLoyaltyTier = "None", DiscountPercent = 20m }));
    }

    [Fact]
    [Trace("REQ-043")]
    public void Req043_ShippingMethodMustBeSupported()
    {
        Assert.True(OrderRules.Req043(Order.Good()));
        Assert.False(OrderRules.Req043(Order.Good() with { ShippingMethod = "Teleport" }));
    }

    [Fact]
    [Trace("REQ-044")]
    public void Req044_ExpressFlagMustMatchShippingMethod()
    {
        Assert.True(OrderRules.Req044(Order.Good()));
        Assert.False(OrderRules.Req044(Order.Good() with { ShippingMethod = "Express" }));
    }

    [Fact]
    [Trace("REQ-045")]
    public void Req045_CarrierIdMustBeKnown()
    {
        Assert.True(OrderRules.Req045(Order.Good()));
        Assert.False(OrderRules.Req045(Order.Good() with { CarrierId = "RandomCo" }));
    }

    [Fact]
    [Trace("REQ-046")]
    public void Req046_TrackingNumberRequiredOnceLabelPrinted()
    {
        Assert.True(OrderRules.Req046(Order.Good()));
        Assert.False(OrderRules.Req046(Order.Good() with { LabelPrinted = true, TrackingNumber = "" }));
    }

    [Fact]
    [Trace("REQ-047")]
    public void Req047_WeightMustBePositive()
    {
        Assert.True(OrderRules.Req047(Order.Good()));
        Assert.False(OrderRules.Req047(Order.Good() with { WeightKg = 0m }));
    }

    [Fact]
    [Trace("REQ-048")]
    public void Req048_OverweightParcelsMustNotUseStandard()
    {
        Assert.True(OrderRules.Req048(Order.Good()));
        Assert.False(OrderRules.Req048(Order.Good() with { WeightKg = 40m }));
    }
}
