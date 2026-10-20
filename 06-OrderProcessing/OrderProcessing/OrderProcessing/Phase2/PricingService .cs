using OrderProcessing.Exceptions;
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2
{
    public class PricingService
    {
        JsonFileStore jsonFileStore = new();
        CustomerRepository customerRepository = new();
        CouponRepository couponRepository = new();
        public async Task<decimal> CalculateSubTotalAsync(Order order, CancellationToken ct)
        {
            Log.Info("PricingService -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("PricingService -> Executing products query...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Product> products = await jsonFileStore.ReadAsync<Product>("products.json", ct);

            decimal subTotal = 0m;
            foreach (var item in order.Items)
            {
                Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product is null)
                {
                    throw new InvalidOperationException($"Product #{item.ProductId} not found");
                }
                item.UnitPrice = product.Price;
                subTotal += item.UnitPrice * item.Quantity;
            }
            order.SubTotal = Math.Round(subTotal, 2, MidpointRounding.AwayFromZero);
            return order.SubTotal;
        }


        public async Task<decimal> GetDiscountAsync(Order order, CancellationToken ct)
        {
            Log.Info("Discount API -> Handshake...");
            await Task.Delay(150, ct);

            Log.Info("Discount API -> Sending request...");
            await Task.Delay(300, ct);

            Log.Info("Discount API -> Waiting for response...");
            await Task.Delay(Random.Shared.Next(500, 1501), ct);


            decimal discount = 0m;
            Customer? customer = await customerRepository.GetByIdAsync(order.CustomerId, ct);
            if (customer is null)
                throw new InvalidOperationException("Customer not found");


            if (!string.IsNullOrWhiteSpace(order.CouponCode))
            {
                Coupon? coupon = await couponRepository.GetByCodeAsync(order.CouponCode, ct);
                if (coupon is null || coupon.ExpiryDate.Date < DateTime.Today)
                    throw new InvalidCouponException("Invalid coupon");
                discount += coupon.DiscountPercent;
            }


            if (customer.LoyaltyTier == LoyaltyTier.Bronze)
            {
                discount += 0m;
            }
            else if (customer.LoyaltyTier == LoyaltyTier.Silver)
            {
                discount += 5m;
            }
            else
            {
                discount += 10m;
            }


            discount = Math.Min(discount, 30m);
            order.Discount = Math.Round(order.SubTotal * discount / 100m, 2, MidpointRounding.AwayFromZero);
            return order.Discount;
        }



        public async Task<decimal> CalculateShippingCostAsync(Order order, CancellationToken ct)
        {
            Log.Info("Shipping API -> Handshake...");
            await Task.Delay(150, ct);

            if (_failShippingForOrderIds.Contains(order.Id))
            {
                throw new ShippingProviderUnavailableException("Shipping provider unavailable");
            }

            Log.Info("Shipping API -> Sending request...");
            await Task.Delay(300, ct);

            Log.Info("Shipping API -> Waiting for response...");
            await Task.Delay(Random.Shared.Next(500, 1501), ct);

            decimal totalQuantity = 0m;
            decimal totalWeight = 0m;

            List<Product> products = await jsonFileStore.ReadAsync<Product>("products.json", ct);

            foreach (var item in order.Items)
            {
                Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product is null)
                {
                    throw new InvalidOperationException($"Product #{item.ProductId} not found");
                }
                totalWeight += product.WeightKg * item.Quantity;
                totalQuantity += item.Quantity;
            }

            if (order.SubTotal >= 1000)
            {
                order.ShippingCost = 0m;
                return 0;
            }
            else
            {
                order.ShippingCost = Math.Round(5 + (1.5m * totalQuantity) + (2 * totalWeight), 2, MidpointRounding.AwayFromZero);
            }
            return order.ShippingCost;
        }
        private readonly HashSet<int> _failShippingForOrderIds;
        public PricingService(IEnumerable<int> failShippingForOrderIds)
        {
            _failShippingForOrderIds = new HashSet<int>(failShippingForOrderIds);
        }
    }
}
