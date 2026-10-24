using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System.Diagnostics;

namespace OrderProcessing.Phase1
{
    public class Phase1Runner
    {
        public async Task RunAsync(CancellationToken ct)
        {
            DataSeeder dataSeeder = new();
            await dataSeeder.ResetAsync(ct);
            IncomingOrderRepository incomingOrderRepository = new();
            List<Order> incomingOrder1 = await incomingOrderRepository.GetAllAsync(ct);
            SyncOrderValidator syncOrderValidator = new();
            Stopwatch stopwatch1 = new Stopwatch();
            stopwatch1.Start();
            foreach (var order in incomingOrder1)
            {
                syncOrderValidator.ValidateOrderSync(order);
            }
            stopwatch1.Stop();




            await dataSeeder.ResetAsync(ct);
            List<Order> incomingOrder2 = await incomingOrderRepository.GetAllAsync(ct);
            OrderValidator orderValidator = new();
            OrderRepository orderRepository = new();
            Stopwatch stopwatch2 = new Stopwatch();
            stopwatch2.Start();
            foreach (var order in incomingOrder2)
            {
                await orderValidator.ValidateOrderAsync(order, ct);
                await orderRepository.SaveAsync(order, ct);
            }
            stopwatch2.Stop();



            Console.WriteLine();
            Console.WriteLine();
            List<Order> orders = await orderRepository.GetAllAsync(ct);
            List<Order> validateOrders = await orderRepository.GetByStatusAsync(OrderStatus.Validated, ct);
            List<Order> rejectedOrders = await orderRepository.GetByStatusAsync(OrderStatus.Rejected, ct);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("===== Validation Summary =====");
            Console.WriteLine($"Total orders: {orders.Count}");
            Console.WriteLine($"Validated: {validateOrders.Count}");
            Console.WriteLine($"Rejected: {rejectedOrders.Count}");
            foreach (var order in rejectedOrders)
            {
                Console.WriteLine($"Order #{order.Id}: {order.FailureReason}");
            }
            Console.WriteLine($"Sync Time: {stopwatch1.Elapsed.TotalSeconds:F1} seconds");
            Console.WriteLine($"Async Time: {stopwatch2.Elapsed.TotalSeconds:F1} seconds");
            Console.WriteLine($"orders.json now contains {orders.Count} records.");
        }
    }
}
