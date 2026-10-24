using System.Collections.Concurrent;
using System.Diagnostics;
using OrderProcessing.Phase3;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase4
{
    public class EmailResendService
    {
        public int MaxConcurrentSends { get; private set; }
        public int SentCount { get; private set; }
        public long ElapsedMs { get; private set; }
        public int DistinctThreads { get; private set; }

        public async Task RunAsync(CancellationToken ct)
        {
            IEmailService emailService = new EmailService();
            JsonFileStore jsonFileStore = new();

            Customer customer = new()
            {
                Id = 1001,
                Name = "Test Customer",
                Email = "test@example.com"
            };

            List<Order> orders = Enumerable.Range(1001, 30)
                .Select(id => new Order
                {
                    Id = id,
                    CustomerId = customer.Id,
                    Status = OrderStatus.Completed,
                    TotalAmount = 100
                })
                .ToList();

            int currentSends = 0;
            int maxConcurrent = 0;

            ConcurrentDictionary<int, byte> threads = new();

            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = ct
            };

            Stopwatch stopwatch = Stopwatch.StartNew();

            await Parallel.ForEachAsync(orders, options, async (order, token) =>
            {
                threads.TryAdd(Environment.CurrentManagedThreadId, 0);

                int current = Interlocked.Increment(ref currentSends);

                int oldMax;

                do
                {
                    oldMax = Volatile.Read(ref maxConcurrent);

                    if (current <= oldMax)
                    {
                        break;
                    }
                }
                while (Interlocked.CompareExchange(ref maxConcurrent, current, oldMax) != oldMax);

                try
                {
                    await emailService.SendConfirmationAsync(order, customer, token);
                }
                finally
                {
                    Interlocked.Decrement(ref currentSends);
                }
            });

            stopwatch.Stop();

            List<EmailMessage> emails = await jsonFileStore.ReadAsync<EmailMessage>("emails.json", ct);

            SentCount = emails.Count(e => e.OrderId >= 1001 && e.OrderId <= 1030);
            MaxConcurrentSends = maxConcurrent;
            ElapsedMs = stopwatch.ElapsedMilliseconds;
            DistinctThreads = threads.Count;
        }
    }
}