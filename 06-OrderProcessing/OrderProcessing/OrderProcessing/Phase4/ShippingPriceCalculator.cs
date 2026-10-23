using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase4
{
    public class ShippingPriceCalculator
    {
        JsonFileStore jsonFileStore = new();

        public async Task<decimal> CalculateAsync(Order order, CancellationToken ct)
        {
            List<Product> products = await jsonFileStore.ReadAsync<Product>("products.json", ct);

            int totalQuantity = order.Items.Sum(i => i.Quantity);

            decimal totalWeight = order.Items.Sum(item =>
            {
                Product? product = products.FirstOrDefault(p => p.Id == item.ProductId);

                return (product?.WeightKg ?? 0) * item.Quantity;
            });

            decimal shippingCost = 5.00m + 1.50m * totalQuantity + 2.00m * totalWeight;

            if (order.SubTotal >= 1000)
            {
                shippingCost = 0;
            }

            return Math.Round(shippingCost, 2, MidpointRounding.AwayFromZero);
        }
    }
}