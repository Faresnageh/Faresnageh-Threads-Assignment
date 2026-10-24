using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1
{
    public class CouponRepository
    {
        public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct)
        {
            Log.Info("CouponRepository -> Opening connection...");
            await Task.Delay(200, ct);


            Log.Info($"CouponRepository -> Executing query for Coupon #{code}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);
            JsonFileStore jsonFileStore = new();
            List<Coupon> coupons = await jsonFileStore.ReadAsync<Coupon>("coupons.json", ct);
            foreach (var coupon in coupons)
            {
                if (string.Equals(coupon.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Success($"CouponRepository -> Code #{coupon.Code}");
                    return coupon;
                }
            }
            return null;
        }
    }
}
