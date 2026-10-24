using OrderProcessing.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1
{
    public class OrderRepository
    {
        JsonFileStore jsonFileStore = new();

        HashSet<int> failSaveForOrderIds;

        public OrderRepository(IEnumerable<int>? failSaveForOrderIds = null)
        {
            this.failSaveForOrderIds = failSaveForOrderIds?.ToHashSet() ?? new();
        }

        public async Task SaveAsync(Order order, CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("OrderRepository -> Beginning transaction...");
            await Task.Delay(100, ct);

            Log.Info($"OrderRepository -> Saving Order #{order.Id}...");
            await Task.Delay(400, ct);

            await jsonFileStore.UpdateAsync<Order>("orders.json", orders =>
            {
                Order? existingOrder = orders.FirstOrDefault(o => o.Id == order.Id);

                if (existingOrder is null)
                {
                    orders.Add(order);
                }
                else
                {
                    int index = orders.IndexOf(existingOrder);
                    orders[index] = order;
                }
            }, ct);

            Log.Info("OrderRepository -> Committing transaction...");
            await Task.Delay(200, ct);

            if (failSaveForOrderIds.Contains(order.Id) && order.Status == OrderStatus.Completed)
            {
                throw new DatabaseWriteException("Connection lost during commit");
            }

            Log.Success($"OrderRepository -> Order #{order.Id} saved");
        }

        public async Task<List<Order>> GetAllAsync(CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("OrderRepository -> Executing query for orders.json...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            return await jsonFileStore.ReadAsync<Order>("orders.json", ct);
        }

        public async Task<List<Order>> GetByStatusAsync(OrderStatus status, CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"OrderRepository -> Executing query for status {status}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Order> orders = await jsonFileStore.ReadAsync<Order>("orders.json", ct);

            return orders.Where(o => o.Status == status).ToList();
        }
    }
}