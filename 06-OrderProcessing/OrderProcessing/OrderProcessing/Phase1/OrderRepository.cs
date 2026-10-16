using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1
{
    public class OrderRepository
    {
        public async Task SaveAsync(Order order, CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);


            Log.Info("OrderRepository -> Beginning transaction...");
            await Task.Delay(100, ct);
            JsonFileStore jsonFileStore = new();

            Log.Info($"OrderRepository -> Saving Order #{order.Id}...");
            await Task.Delay(400, ct);
            await jsonFileStore.UpdateAsync<Order>("orders.json",orders=>
            {
                int index = orders.FindIndex(o => o.Id == order.Id);
                if (index == -1)
                {
                    orders.Add(order);
                }
                else
                {
                    orders[index] = order;
                }
            },ct);
            Log.Info("OrderRepository -> Committing transaction...");
            await Task.Delay(200, ct);
        }



        public async Task<List<Order>> GetAllAsync(CancellationToken ct)
        {
            Log.Info("OrderRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"OrderRepository -> Executing query for orders.json...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);


            JsonFileStore jsonFileStore = new();
            List<Order> orders = await jsonFileStore.ReadAsync<Order>("orders.json", ct);
            return orders;
        }



        public async Task<List<Order>> GetByStatusAsync(OrderStatus s, CancellationToken ct)
        {
            List<Order> orders = await GetAllAsync(ct);
            return orders.Where(orders => orders.Status == s).ToList();
        }
    }
}
