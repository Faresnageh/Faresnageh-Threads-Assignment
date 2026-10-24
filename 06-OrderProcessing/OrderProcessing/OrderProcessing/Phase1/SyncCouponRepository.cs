using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1
{
    public class SyncCouponRepository
    {
        public Coupon? GetByCodeSync(string code)
        {
            Log.Info("SyncCouponRepository -> Opening connection...");
            Thread.Sleep(200);


            Log.Info($"SyncCouponRepository -> Executing query for Coupon #{code}...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            JsonFileStore jsonFileStore = new();
            List<Coupon> coupons =  jsonFileStore.Read<Coupon>("coupons.json");
            foreach (var coupon in coupons)
            {
                if (string.Equals(coupon.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Success($"SyncCouponRepository -> Code #{coupon.Code}");
                    return coupon;
                }
            }
            return null;
        }
    }
}
