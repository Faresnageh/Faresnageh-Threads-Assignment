using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase2
{
    public class OrderPricer
    {
        private readonly PricingService pricingService;
        public OrderPricer(PricingService pricingService)
        {
            this.pricingService = pricingService;
        }

        public async Task<decimal> PriceOrderSequentialAsync(Order order, CancellationToken ct)
        {
            decimal subTotal = await pricingService.CalculateSubTotalAsync(order, ct);
            decimal discount = await pricingService.GetDiscountAsync(order, ct);
            decimal shippingCost = await pricingService.CalculateShippingCostAsync(order, ct);
            order.TotalAmount = Math.Round((subTotal - discount + shippingCost), 2, MidpointRounding.AwayFromZero);
            return order.TotalAmount;
        }

        public async Task<decimal> PriceOrderAsync(Order order, CancellationToken ct)
        {
            decimal subTotal = await pricingService.CalculateSubTotalAsync(order, ct);
            Task<decimal> discountTask = pricingService.GetDiscountAsync(order, ct);
            Task<decimal> shippingCostTask = pricingService.CalculateShippingCostAsync(order, ct);
            order.TotalAmount = Math.Round((subTotal - (await discountTask) + (await shippingCostTask)), 2, MidpointRounding.AwayFromZero);
            return order.TotalAmount;
        }
    }
}
