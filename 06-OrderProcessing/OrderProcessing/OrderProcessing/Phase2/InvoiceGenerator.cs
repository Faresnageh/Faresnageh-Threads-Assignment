
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase2
{
    public class InvoiceGenerator
    {
        public async Task GenerateAsync(Order order, CancellationToken ct)
        {
            Log.Info($"InvoiceGenerator -> Rendering invoice for Order #{order.Id}...");


            long result = await Task.Run(() =>
            {
                long sum = 0;
                for (int i = 0; i < 20_000_000; i++)
                {
                    if (i % 100_000 == 0)
                        ct.ThrowIfCancellationRequested();
                    sum += i % 100;
                }
                return sum;
            }, ct);


            Log.Info($"InvoiceGenerator -> Rendering finished: {result}");

            string directory = Path.Combine(AppContext.BaseDirectory, "Data", "invoices");
            Directory.CreateDirectory(directory);

            string filePath = Path.Combine(directory,$"invoice_{order.Id}.txt");

            string content = $"""
                Invoice - Order #{order.Id}
                -----------------------------
                Customer ID: {order.CustomerId}
                Subtotal: {order.SubTotal:F2}
                Discount: {order.Discount:F2}
                Shipping: {order.ShippingCost:F2}
                Total: {order.TotalAmount:F2}
                """;

            await File.WriteAllTextAsync(filePath, content, ct);
            Log.Success($"InvoiceGenerator -> Invoice saved: {filePath}");
        }
    }
}
