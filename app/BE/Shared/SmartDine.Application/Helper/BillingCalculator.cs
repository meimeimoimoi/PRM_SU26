namespace SmartDine.Application.Helper;

/// <summary>
/// Tính phí dịch vụ + VAT. Ưu tiên snapshot trên DiningSession; phiên cũ (null) fallback settings.
/// </summary>
public static class BillingCalculator
{
    public static (decimal ServiceCharge, decimal Tax, decimal Total) Compute(
        decimal subTotal,
        decimal taxRatePercent,
        decimal serviceChargeRatePercent)
    {
        if (subTotal <= 0)
            return (0, 0, 0);

        var service = Math.Round(subTotal * serviceChargeRatePercent / 100m, 0, MidpointRounding.AwayFromZero);
        var tax = Math.Round(subTotal * taxRatePercent / 100m, 0, MidpointRounding.AwayFromZero);
        return (service, tax, subTotal + service + tax);
    }

    /// <summary>
    /// Snapshot phiên hiện tại nếu có; không thì dùng RestaurantSettings (phiên legacy).
    /// </summary>
    public static (decimal TaxRate, decimal ServiceChargeRate) ResolveRates(
        decimal? sessionTaxRate,
        decimal? sessionServiceChargeRate,
        decimal settingsTaxRate,
        decimal settingsServiceChargeRate)
        => (sessionTaxRate ?? settingsTaxRate, sessionServiceChargeRate ?? settingsServiceChargeRate);
}
