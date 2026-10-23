using System.Collections.Concurrent;
using System.Diagnostics;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4
{
    public class FraudScoringService
    {
        public FraudScoringResult Run(CancellationToken ct)
        {
            List<Order> orders = Enumerable.Range(1, 10_000).Select(id => new Order { Id = id })
                .ToList();

            List<int> sequentialFlagged = new();

            Stopwatch sequentialWatch = Stopwatch.StartNew();

            foreach (Order order in orders)
            {
                ct.ThrowIfCancellationRequested();

                int score = Score(order.Id);

                if (score >= 90)
                {
                    sequentialFlagged.Add(order.Id);
                }
            }

            sequentialWatch.Stop();

            ConcurrentBag<int> parallelFlagged = new();
            ConcurrentDictionary<int, byte> threads = new();

            ParallelOptions options = new()
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = ct
            };

            Stopwatch parallelWatch = Stopwatch.StartNew();

            Parallel.ForEach(orders, options, order =>
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                threads.TryAdd(Environment.CurrentManagedThreadId, 0);

                int score = Score(order.Id);

                if (score >= 90)
                {
                    parallelFlagged.Add(order.Id);
                }
            });

            parallelWatch.Stop();

            List<int> parallelIds = parallelFlagged.OrderBy(id => id).ToList();

            if (!sequentialFlagged.SequenceEqual(parallelIds))
            {
                throw new InvalidOperationException("Sequential and parallel fraud results do not match");
            }

            return new FraudScoringResult
            {
                FlaggedCount = parallelIds.Count,
                FlaggedIds = parallelIds,
                SequentialMs = sequentialWatch.ElapsedMilliseconds,
                ParallelMs = parallelWatch.ElapsedMilliseconds,
                SpeedUp = parallelWatch.ElapsedMilliseconds == 0 ? 0
                    : (double)sequentialWatch.ElapsedMilliseconds / parallelWatch.ElapsedMilliseconds,
                DistinctThreads = threads.Count,
                ProcessorCount = Environment.ProcessorCount
            };
        }

        private static int Score(int orderId)
        {
            long h = orderId;

            for (int i = 0; i < 100_000; i++)
            {
                h = (h * 31 + i) % 1_000_000_007;
            }

            return (int)(h % 100);
        }
    }
}