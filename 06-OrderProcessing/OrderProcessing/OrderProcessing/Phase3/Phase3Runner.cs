using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3
{
    public class Phase3Runner
    {
        public async Task RunAsync(CancellationToken ct)
        {
            DataSeeder seeder = new();

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

            Console.WriteLine("===== PHASE 3 =====");

            await seeder.ResetAsync(ct);

            Console.WriteLine();
            Console.WriteLine("===== VALIDATION =====");

            List<Order> orders = await incomingOrderRepository.GetAllAsync(ct);

            List<Task> validationTasks = new();

            foreach (Order order in orders)
            {
                validationTasks.Add(ValidateAndSaveAsync(order, orderValidator, orderRepository, ct));
            }

            foreach (Task task in validationTasks)
            {
                await task;
            }

            Console.WriteLine($"Validated: {orders.Count(o => o.Status == OrderStatus.Validated)}");
            Console.WriteLine($"Rejected: {orders.Count(o => o.Status == OrderStatus.Rejected)}");

            Console.WriteLine();
            Console.WriteLine("===== ORDER PROCESSING =====");

            List<Order> validatedOrders = orders.Where(o => o.Status == OrderStatus.Validated).ToList();

            List<Task<bool>> processingTasks = new();

            foreach (Order order in validatedOrders)
            {
                Task<bool> task = orderProcessor.ProcessAsync(order, ct);
                processingTasks.Add(task);
            }

            for (int i = 0; i < processingTasks.Count; i++)
            {
                Order order = validatedOrders[i];
                Task<bool> task = processingTasks[i];

                try
                {
                    bool success = await task;

                    Console.WriteLine($"Order #{order.Id}: {order.Status}, Success: {success}");
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    Console.WriteLine($"Order #{order.Id}: Cancelled");
                }
            }

            Console.WriteLine();
            Console.WriteLine("===== FINAL ORDERS =====");

            List<Order> savedOrders = await orderRepository.GetAllAsync(ct);

            Console.WriteLine($"Total Orders: {savedOrders.Count}");
            Console.WriteLine($"Completed: {savedOrders.Count(o => o.Status == OrderStatus.Completed)}");
            Console.WriteLine($"Rejected: {savedOrders.Count(o => o.Status == OrderStatus.Rejected)}");
            Console.WriteLine($"Payment Failed: {savedOrders.Count(o => o.Status == OrderStatus.PaymentFailed)}");
            Console.WriteLine($"Failed: {savedOrders.Count(o => o.Status == OrderStatus.Failed)}");

            foreach (Order order in savedOrders.OrderBy(o => o.Id))
            {
                Console.WriteLine($"Order #{order.Id} | {order.Status} | Total: {order.TotalAmount:F2} | {order.FailureReason}");
            }

            JsonFileStore jsonFileStore = new();

            List<PaymentTransaction> payments = await jsonFileStore.ReadAsync<PaymentTransaction>("payments.json", ct);
            List<EmailMessage> emails = await jsonFileStore.ReadAsync<EmailMessage>("emails.json", ct);
            List<EmailMessage> emailRetry = await jsonFileStore.ReadAsync<EmailMessage>("email_retry.json", ct);

            Console.WriteLine();
            Console.WriteLine("===== PAYMENTS =====");

            Console.WriteLine($"Captured: {payments.Count(p => p.Status == PaymentStatus.Captured)}");
            Console.WriteLine($"Declined: {payments.Count(p => p.Status == PaymentStatus.Declined)}");
            Console.WriteLine($"Refunded: {payments.Count(p => p.Status == PaymentStatus.Refunded)}");

            Console.WriteLine();
            Console.WriteLine("===== EMAILS =====");

            Console.WriteLine($"Sent Emails: {emails.Count}");
            Console.WriteLine($"Email Retry: {emailRetry.Count}");

            EmailMessage? order3Email = emails.FirstOrDefault(e => e.OrderId == 3);
            EmailMessage? order17Retry = emailRetry.FirstOrDefault(e => e.OrderId == 17);

            if (order3Email is not null)
            {
                Console.WriteLine($"Order #3 Email Attempts: {order3Email.Attempts}");
            }

            if (order17Retry is not null)
            {
                Console.WriteLine($"Order #17 Email Attempts: {order17Retry.Attempts}");
            }

            SyncVsAsyncDemo syncVsAsyncDemo = new();
            await syncVsAsyncDemo.RunAsync();

            FireAndForgetDemo fireAndForgetDemo = new();
            await fireAndForgetDemo.RunAsync();
        }

        private static async Task ValidateAndSaveAsync(Order order, OrderValidator orderValidator, OrderRepository orderRepository, CancellationToken ct)
        {
            await orderValidator.ValidateOrderAsync(order, ct);
            await orderRepository.SaveAsync(order, ct);
        }
    }
}