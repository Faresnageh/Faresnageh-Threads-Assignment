using OrderProcessing.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4
{
    public class CairoExpress : IShippingCarrier
    {
        HashSet<int> failForOrderIds;
        ShippingPriceCalculator priceCalculator = new();

        public string Name => "CairoExpress";

        public CairoExpress(IEnumerable<int>? failForOrderIds = null)
        {
            this.failForOrderIds = failForOrderIds?.ToHashSet() ?? new();
        }

        public async Task<decimal> GetQuoteAsync(Order order, CancellationToken ct)
        {
            try
            {
                Log.Info($"[{Name}] handshake for Order #{order.Id}...");
                await Task.Delay(150, ct);

                if (failForOrderIds.Contains(order.Id))
                {
                    throw new ShippingProviderUnavailableException($"{Name} unavailable");
                }

                Log.Info($"[{Name}] getting quote for Order #{order.Id}...");
                await Task.Delay(Random.Shared.Next(300, 2001), ct);

                if (order.Id % 2 == 0)
                {
                    throw new ShippingProviderUnavailableException($"{Name} unavailable");
                }

                decimal basePrice = await priceCalculator.CalculateAsync(order, ct);

                return Math.Round(basePrice * 0.95m, 2, MidpointRounding.AwayFromZero);
            }
            catch (OperationCanceledException)
            {
                Log.Info($"[{Name}] cancelled");
                throw;
            }
        }
    }
}