using System.Diagnostics;
using OrderProcessing.Exceptions;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4
{
    public class ShippingQuoteService
    {
        List<IShippingCarrier> carriers;

        public List<string> LastCancelledCarriers { get; private set; } = new();

        public ShippingQuoteService(IEnumerable<IShippingCarrier> carriers)
        {
            this.carriers = carriers.ToList();
        }

        public async Task<ShippingQuote> GetFastestQuoteAsync(Order order, CancellationToken ct)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            using CancellationTokenSource carrierCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            Dictionary<Task<(string Carrier, decimal Price)>, IShippingCarrier> taskCarriers = new();

            foreach (IShippingCarrier carrier in carriers)
            {
                Task<(string Carrier, decimal Price)> task = GetQuoteAsync(carrier, order, carrierCts.Token);
                taskCarriers.Add(task, carrier);
            }

            List<Task<(string Carrier, decimal Price)>> remainingTasks = taskCarriers.Keys.ToList();

            Task timeoutTask = Task.Delay(1200, ct);

            LastCancelledCarriers = new();

            int failedCarriers = 0;

            while (remainingTasks.Count > 0)
            {
                List<Task> waitTasks = remainingTasks.Cast<Task>().ToList();
                waitTasks.Add(timeoutTask);

                Task finishedTask = await Task.WhenAny(waitTasks);

                if (finishedTask == timeoutTask)
                {
                    await timeoutTask;

                    LastCancelledCarriers = remainingTasks
                        .Where(t => !t.IsCompleted)
                        .Select(t => taskCarriers[t].Name)
                        .ToList();

                    carrierCts.Cancel();

                    await ObserveTasksAsync(remainingTasks);

                    stopwatch.Stop();

                    return new ShippingQuote
                    {
                        Carrier = "Flat rate",
                        Price = 25.00m,
                        ElapsedMs = stopwatch.ElapsedMilliseconds
                    };
                }

                Task<(string Carrier, decimal Price)> completedTask = (Task<(string Carrier, decimal Price)>)finishedTask;

                remainingTasks.Remove(completedTask);

                try
                {
                    (string carrier, decimal price) = await completedTask;

                    if (stopwatch.ElapsedMilliseconds >= 1200)
                    {
                        LastCancelledCarriers = remainingTasks
                            .Where(t => !t.IsCompleted)
                            .Select(t => taskCarriers[t].Name)
                            .ToList();

                        carrierCts.Cancel();

                        await ObserveTasksAsync(remainingTasks);

                        stopwatch.Stop();

                        return new ShippingQuote
                        {
                            Carrier = "Flat rate",
                            Price = 25.00m,
                            ElapsedMs = stopwatch.ElapsedMilliseconds
                        };
                    }

                    LastCancelledCarriers = remainingTasks
                        .Where(t => !t.IsCompleted)
                        .Select(t => taskCarriers[t].Name)
                        .ToList();

                    carrierCts.Cancel();

                    await ObserveTasksAsync(remainingTasks);

                    stopwatch.Stop();

                    return new ShippingQuote
                    {
                        Carrier = carrier,
                        Price = price,
                        ElapsedMs = stopwatch.ElapsedMilliseconds
                    };
                }
                catch (ShippingProviderUnavailableException)
                {
                    failedCarriers++;

                    if (failedCarriers == carriers.Count)
                    {
                        stopwatch.Stop();

                        throw new ShippingProviderUnavailableException("All carriers failed");
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    carrierCts.Cancel();
                    throw;
                }
            }

            throw new ShippingProviderUnavailableException("All carriers failed");
        }

        private static async Task<(string Carrier, decimal Price)> GetQuoteAsync(IShippingCarrier carrier, Order order, CancellationToken ct)
        {
            decimal price = await carrier.GetQuoteAsync(order, ct);

            return (carrier.Name, price);
        }

        private static async Task ObserveTasksAsync(IEnumerable<Task<(string Carrier, decimal Price)>> tasks)
        {
            foreach (Task task in tasks)
            {
                try
                {
                    await task;
                }
                catch
                {
                }
            }
        }
    }
}