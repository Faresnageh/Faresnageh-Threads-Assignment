using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System.Diagnostics;

namespace OrderProcessing.Phase2
{
    public class Phase2Runner
    {
        public async Task RunAsync(CancellationToken ct)
        {
            DataSeeder seeder = new();

            ProductRepository productRepository = new();
            OrderRepository orderRepository = new();
            IncomingOrderRepository incomingOrderRepository = new();
            OrderValidator orderValidator = new();
            PricingService pricingService = new(new[] { 7 });
            OrderPricer orderPricer = new(pricingService);
            InvoiceGenerator invoiceGenerator = new();
            OrderProcessorV2 orderProcessor = new(productRepository, orderRepository, orderPricer, invoiceGenerator);
            ReportGenerator reportGenerator = new(orderRepository, productRepository);

            Console.WriteLine("===== RACE CONDITION DEMO =====");

            await seeder.ResetAsync(ct);

            RaceConditionDemo raceDemo = new();
            await raceDemo.RunAsync(ct);

            Console.WriteLine();
            Console.WriteLine("===== RESET DATA =====");

            await seeder.ResetAsync(ct);

            Console.WriteLine();
            Console.WriteLine("===== PRICING COMPARISON =====");

            List<Order> sequentialOrders = await incomingOrderRepository.GetAllAsync(ct);
            Order sequentialOrder = sequentialOrders.First(o => o.Id == 1);

            Stopwatch sequentialWatch = Stopwatch.StartNew();
            decimal sequentialTotal = await orderPricer.PriceOrderSequentialAsync(sequentialOrder, ct);
            sequentialWatch.Stop();

            List<Order> concurrentOrders = await incomingOrderRepository.GetAllAsync(ct);
            Order concurrentOrder = concurrentOrders.First(o => o.Id == 1);

            Stopwatch concurrentWatch = Stopwatch.StartNew();
            decimal concurrentTotal = await orderPricer.PriceOrderAsync(concurrentOrder, ct);
            concurrentWatch.Stop();

            Console.WriteLine($"Sequential Total: {sequentialTotal:F2}");
            Console.WriteLine($"Sequential Time: {sequentialWatch.Elapsed.TotalSeconds:F2}s");

            Console.WriteLine($"Concurrent Total: {concurrentTotal:F2}");
            Console.WriteLine($"Concurrent Time: {concurrentWatch.Elapsed.TotalSeconds:F2}s");

            Console.WriteLine();
            Console.WriteLine("===== CONCURRENT VALIDATION =====");

            List<Order> orders = await incomingOrderRepository.GetAllAsync(ct);
            List<Task> validationTasks = new();

            foreach (Order order in orders)
            {
                if (ct.IsCancellationRequested)
                    break;

                validationTasks.Add(ValidateAndSaveAsync(order, orderValidator, orderRepository, ct));
            }


            bool validationCancelled = false;
            foreach (Task task in validationTasks)
            {
                try
                {
                    await task;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    validationCancelled = true;
                }
            }

            if (validationCancelled || ct.IsCancellationRequested)
            {
                await SaveCancellationSummaryAsync(orders, orderRepository);
                return;
            }


            Console.WriteLine($"Validated: {orders.Count(o => o.Status == OrderStatus.Validated)}");
            Console.WriteLine($"Rejected: {orders.Count(o => o.Status == OrderStatus.Rejected)}");

            Console.WriteLine();
            Console.WriteLine("===== CONCURRENT ORDER PROCESSING =====");

            List<Order> validatedOrders = orders.Where(o => o.Status == OrderStatus.Validated).ToList();

            List<Order> processingOrders = new();
            List<Task<bool>> processingTasks = new();

            Stopwatch processingWatch = Stopwatch.StartNew();

            foreach (Order order in validatedOrders)
            {
                if (ct.IsCancellationRequested)
                    break;

                processingOrders.Add(order);
                processingTasks.Add(orderProcessor.ProcessValidatedOrderAsync(order, ct));
            }

            if (processingTasks.Count > 0)
            {
                Console.WriteLine($"Task[0] immediately: {processingTasks[0].Status}");
            }

            try
            {
                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                Console.WriteLine("Cancellation requested. Waiting for running orders...");
            }

            Console.WriteLine();
            Console.WriteLine("===== TASK STATUS AFTER 1 SECOND =====");

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Console.WriteLine($"Order #{processingOrders[i].Id}: {processingTasks[i].Status}");
            }

            Console.WriteLine();
            Console.WriteLine("===== PROCESSING RESULTS =====");

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Order order = processingOrders[i];
                Task<bool> task = processingTasks[i];

                try
                {
                    bool success = await task;

                    Console.WriteLine($"Order #{order.Id}: {order.Status}, Success: {success}");
                }
                catch (Exception ex)
                {

                    if (ct.IsCancellationRequested)
                    {
                        order.Status = OrderStatus.Pending;
                        order.FailureReason = "Cancelled";
                    }
                    else
                    {
                        order.Status = OrderStatus.Failed;
                        order.FailureReason = ex.Message;
                    }

                    await orderRepository.SaveAsync(order, CancellationToken.None);

                    Console.WriteLine($"Order #{order.Id}: {task.Status} - {order.FailureReason}");
                }
            }

            processingWatch.Stop();

            if (ct.IsCancellationRequested)
            {
                await SaveCancellationSummaryAsync(orders, orderRepository);
                return;
            }

            Console.WriteLine();
            Console.WriteLine("===== FINAL TASK STATUS =====");

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Console.WriteLine($"Order #{processingOrders[i].Id}: {processingTasks[i].Status}");
            }

            Console.WriteLine($"Processing Time: {processingWatch.Elapsed.TotalSeconds:F2}s");

            Console.WriteLine();
            Console.WriteLine("===== FINAL ORDER SUMMARY =====");

            List<Order> savedOrders = await orderRepository.GetAllAsync(ct);

            Console.WriteLine($"Total Orders: {savedOrders.Count}");
            Console.WriteLine($"Completed: {savedOrders.Count(o => o.Status == OrderStatus.Completed)}");
            Console.WriteLine($"Rejected: {savedOrders.Count(o => o.Status == OrderStatus.Rejected)}");
            Console.WriteLine($"Failed: {savedOrders.Count(o => o.Status == OrderStatus.Failed)}");

            foreach (Order order in savedOrders.OrderBy(o => o.Id))
            {
                Console.WriteLine($"Order #{order.Id} | {order.Status} | Total: {order.TotalAmount:F2} | {order.FailureReason}");
            }

            Console.WriteLine();
            Console.WriteLine("===== REPORT GENERATION =====");

            Task<Report> reportTask = reportGenerator.GenerateAsync(ct);

            Console.WriteLine("Generating report...");

            Report report = await reportTask;

            Console.WriteLine($"Total Orders: {report.TotalOrders}");
            Console.WriteLine($"Completed: {report.CompletedCount}");
            Console.WriteLine($"Rejected: {report.RejectedCount}");
            Console.WriteLine($"Failed: {report.FailedCount}");
            Console.WriteLine($"Total Revenue: {report.TotalRevenue:F2}");
            Console.WriteLine($"Average Order Value: {report.AverageOrderValue:F2}");
            Console.WriteLine($"Top Selling Product: {report.TopSellingProduct} ({report.TopSellingQuantity})");
        }

        private static async Task ValidateAndSaveAsync(Order order, OrderValidator validator, OrderRepository orderRepository, CancellationToken ct)
        {
            await validator.ValidateOrderAsync(order, ct);
            await orderRepository.SaveAsync(order, ct);
        }

        private static async Task SaveCancellationSummaryAsync(List<Order> orders, OrderRepository orderRepository)
        {
            List<Task> saveTasks = new();

            foreach (Order order in orders)
            {
                if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Validated)
                {
                    order.Status = OrderStatus.Pending;
                    order.FailureReason = "Cancelled";
                }

                saveTasks.Add(orderRepository.SaveAsync(order, CancellationToken.None));
            }

            foreach (Task task in saveTasks)
            {
                await task;
            }

            int cancelledCount = orders.Count(o => o.Status == OrderStatus.Pending && o.FailureReason == "Cancelled");
            int finishedCount = orders.Count - cancelledCount;

            Console.WriteLine($"Run cancelled — {finishedCount} orders finished, {cancelledCount} cancelled.");
        }
    }
}
