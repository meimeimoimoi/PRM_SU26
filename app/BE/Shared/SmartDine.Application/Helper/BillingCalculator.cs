namespace SmartDine.Application.Helper;

/// <summary>
/// Tính phí dịch vụ + VAT từ RestaurantSettings (TaxRate / ServiceChargeRate là %).
/// VD: TaxRate=10, ServiceChargeRate=5, subTotal=100000 → service=5000, tax=10000, total=115000.
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
}
