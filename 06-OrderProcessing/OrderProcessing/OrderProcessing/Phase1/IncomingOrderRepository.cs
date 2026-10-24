using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1
{
    public class IncomingOrderRepository
    {
        public async Task<List<Order>> GetAllAsync(CancellationToken ct)
        {
            Log.Info("IncomingOrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"IncomingOrderRepository -> Executing query for incoming_orders.json...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);


            JsonFileStore jsonFileStore = new();
            List<Order> orders = await jsonFileStore.ReadAsync<Order>("incoming_orders.json", ct);
            return orders;
        }
    }
}
