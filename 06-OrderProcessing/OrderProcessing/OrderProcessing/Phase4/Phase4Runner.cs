using OrderProcessing.Exceptions;
using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System.Diagnostics;

namespace OrderProcessing.Phase4
{
    public class Phase4Runner
    {
        public async Task RunAsync(CancellationToken ct)
        {
            Console.WriteLine("===== PHASE 4 =====");
            Console.WriteLine();
            Console.WriteLine("===== TASK 10.1 - LIVE CHECKOUT PROGRESS =====");

            DataSeeder seeder = new();
            await seeder.ResetAsync(ct);

            ProductRepository productRepository = new();
            OrderRepository orderRepository = new([13]);
            CustomerRepository customerRepository = new();
            IncomingOrderRepository incomingOrderRepository = new();
            OrderValidator orderValidator = new();

            PricingService pricingService = new([]);
            OrderPricer orderPricer = new(pricingService);

            PaymentGateway paymentGateway = new([5], [8]);

            EmailService emailService = new(new Dictionary<int, int>
            {
                [3] = 2,
                [17] = int.MaxValue
            });

            OrderProcessor orderProcessor = new(productRepository, orderRepository, customerRepository, orderPricer, paymentGateway, emailService);

            List<Order> orders = await incomingOrderRepository.GetAllAsync(ct);

            List<Task<LiveCheckoutResult>> tasks = new();

            foreach (Order order in orders)
            {
                tasks.Add(ProcessOrderAsync(order, orderValidator, orderRepository, orderProcessor, ct));
            }

            int finished = 0;

            await foreach (Task<LiveCheckoutResult> completedTask in Task.WhenEach(tasks))
            {
                LiveCheckoutResult result = await completedTask;

                finished++;

                string reason = string.IsNullOrWhiteSpace(result.Order.FailureReason) ? "" : $" ({result.Order.FailureReason})";

                Console.WriteLine($"[{finished,2}/{orders.Count}] Order #{result.Order.Id} -> {result.Order.Status}{reason} after {result.ElapsedSeconds:F1} s");
            }

            Console.WriteLine();
            Console.WriteLine("===== STATUS COUNTS =====");

            List<Order> savedOrders = await orderRepository.GetAllAsync(ct);

            Console.WriteLine($"Completed: {savedOrders.Count(o => o.Status == OrderStatus.Completed)}");
            Console.WriteLine($"Rejected: {savedOrders.Count(o => o.Status == OrderStatus.Rejected)}");
            Console.WriteLine($"PaymentFailed: {savedOrders.Count(o => o.Status == OrderStatus.PaymentFailed)}");
            Console.WriteLine($"Failed: {savedOrders.Count(o => o.Status == OrderStatus.Failed)}");

            Console.WriteLine();
            Console.WriteLine("===== TASK 10.2 - CUSTOMER DASHBOARD =====");

            CustomerDashboardService dashboardService = new([99]);

            await RunDashboardAsync(dashboardService, 3, ct);
            await RunDashboardAsync(dashboardService, 99, ct);

            Console.WriteLine();
            Console.WriteLine("===== TASK 10.3 - FASTEST SHIPPING QUOTE =====");

            await RunShippingQuotesAsync(savedOrders, ct);

            Console.WriteLine();
            Console.WriteLine("===== TASK 10.3 - ALL CARRIERS FAIL =====");

            await RunAllCarriersFailAsync(savedOrders, ct);
            Console.WriteLine();
            Console.WriteLine("===== TASK 10.4 A - FRAUD SCORING =====");

            RunFraudScoring(ct);

            Console.WriteLine();
            Console.WriteLine("===== TASK 10.4 B - EMAIL RE-SEND =====");

            EmailResendService emailResendService = new();
            await emailResendService.RunAsync(ct);

            Console.WriteLine($"Sent Emails: {emailResendService.SentCount}");
            Console.WriteLine($"Max Concurrent Sends: {emailResendService.MaxConcurrentSends}");
            Console.WriteLine($"Total Time: {emailResendService.ElapsedMs} ms");
            Console.WriteLine($"Distinct Threads: {emailResendService.DistinctThreads}");
            Console.WriteLine($"ThreadPool Threads: {ThreadPool.ThreadCount}");
        }

        private static async Task<LiveCheckoutResult> ProcessOrderAsync(Order order, OrderValidator orderValidator, OrderRepository orderRepository, OrderProcessor orderProcessor, CancellationToken ct)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            await orderValidator.ValidateOrderAsync(order, ct);
            await orderRepository.SaveAsync(order, ct);

            bool success = false;

            if (order.Status == OrderStatus.Validated)
            {
                success = await orderProcessor.ProcessAsync(order, ct);
            }

            stopwatch.Stop();

            return new LiveCheckoutResult
            {
                Order = order,
                Success = success,
                ElapsedSeconds = stopwatch.Elapsed.TotalSeconds
            };
        }

        private static async Task RunDashboardAsync(CustomerDashboardService dashboardService, int customerId, CancellationToken ct)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                CustomerDashboard dashboard = await dashboardService.LoadAsync(customerId, ct);

                stopwatch.Stop();

                Console.WriteLine();
                Console.WriteLine($"Customer #{customerId}: {dashboard.Customer.Name}");
                Console.WriteLine($"Orders: {dashboard.Orders.Count}");

                foreach (Order order in dashboard.Orders.OrderBy(o => o.Id))
                {
                    Console.WriteLine($"  Order #{order.Id} -> {order.Status}");
                }

                Console.WriteLine($"Payments: {dashboard.Payments.Count}");

                foreach (PaymentTransaction payment in dashboard.Payments.OrderBy(p => p.OrderId))
                {
                    Console.WriteLine($"  Order #{payment.OrderId} -> {payment.Status}");
                }

                Console.WriteLine($"Emails: {dashboard.Emails.Count}");

                foreach (EmailMessage email in dashboard.Emails.OrderBy(e => e.OrderId))
                {
                    Console.WriteLine($"  Order #{email.OrderId}");
                }

                Console.WriteLine($"Dashboard loaded in {stopwatch.Elapsed.TotalSeconds:F2} s");
            }
            catch (AggregateException ex)
            {
                stopwatch.Stop();

                Console.WriteLine();
                Console.WriteLine($"Customer #{customerId} dashboard failed:");

                foreach (Exception error in ex.InnerExceptions)
                {
                    Console.WriteLine($"  - {error.Message}");
                }

                Console.WriteLine($"Dashboard finished in {stopwatch.Elapsed.TotalSeconds:F2} s");
            }
        }

        private static async Task RunShippingQuotesAsync(List<Order> orders, CancellationToken ct)
        {
            ShippingQuoteService shippingQuoteService = new(
            [
                new FastShip(),
                new CairoExpress(),
                new NileCourier()
            ]);

            for (int orderId = 1; orderId <= 5; orderId++)
            {
                Order order = orders.First(o => o.Id == orderId);

                ShippingQuote quote = await shippingQuoteService.GetFastestQuoteAsync(order, ct);

                string cancelled = shippingQuoteService.LastCancelledCarriers.Count == 0 ? "None" : string.Join(", ", shippingQuoteService.LastCancelledCarriers);

                Console.WriteLine($"Order #{order.Id} -> {quote.Carrier} | Price: {quote.Price:F2} | Time: {quote.ElapsedMs} ms | Cancelled: {cancelled}");
            }
        }

        private static async Task RunAllCarriersFailAsync(List<Order> orders, CancellationToken ct)
        {
            ShippingQuoteService shippingQuoteService = new(
            [
                new FastShip([6]),
                new CairoExpress([6]),
                new NileCourier([6])
            ]);

            Order order = orders.First(o => o.Id == 6);

            try
            {
                await shippingQuoteService.GetFastestQuoteAsync(order, ct);
            }
            catch (ShippingProviderUnavailableException ex)
            {
                Console.WriteLine($"Order #6 -> {ex.Message}");
            }
        }
        private static void RunFraudScoring(CancellationToken ct)
        {
            FraudScoringService fraudScoringService = new();

            FraudScoringResult result = fraudScoringService.Run(ct);

            Console.WriteLine($"Flagged Count: {result.FlaggedCount}");
            Console.WriteLine($"First Five: {string.Join(", ", result.FlaggedIds.Take(5))}");
            Console.WriteLine($"Sequential Time: {result.SequentialMs} ms");
            Console.WriteLine($"Parallel Time: {result.ParallelMs} ms");
            Console.WriteLine($"Speed-up: {result.SpeedUp:F2}x");
            Console.WriteLine($"Distinct Threads: {result.DistinctThreads}");
            Console.WriteLine($"Processor Count: {result.ProcessorCount}");
        }
    }
}