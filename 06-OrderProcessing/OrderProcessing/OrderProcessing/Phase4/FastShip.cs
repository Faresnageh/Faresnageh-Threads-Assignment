using OrderProcessing.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4
{
    public class FastShip : IShippingCarrier
    {
        ShippingPriceCalculator priceCalculator = new();
        HashSet<int> failForOrderIds;

        public string Name => "FastShip";

        public FastShip(IEnumerable<int>? failForOrderIds = null)
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
                await Task.Delay(Random.Shared.Next(500, 1501), ct);


                decimal basePrice = await priceCalculator.CalculateAsync(order, ct);

                return Math.Round(basePrice * 1.00m, 2, MidpointRounding.AwayFromZero);
            }
            catch (OperationCanceledException)
            {
                Log.Info($"[{Name}] cancelled");
                throw;
            }
        }
    }
}