using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;
using OrderProcessing.Phase4;

namespace OrderProcessing
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using CancellationTokenSource cts = new();

            ConsoleCancelEventHandler cancelHandler = (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();

                Console.WriteLine();
                Console.WriteLine("Cancellation requested...");
            };

            Console.CancelKeyPress += cancelHandler;

            try
            {
                int cancelIndex = Array.IndexOf(args, "--cancel-after");

                if (cancelIndex != -1)
                {
                    if (cancelIndex + 1 >= args.Length ||
                        !int.TryParse(args[cancelIndex + 1], out int milliseconds) ||
                        milliseconds <= 0)
                    {
                        Console.WriteLine("Invalid --cancel-after value");
                        return;
                    }

                    cts.CancelAfter(milliseconds);
                }

                string phase = args.Length > 0 && !args[0].StartsWith("--") ? args[0].ToLowerInvariant() : "phase2";

                if (phase == "phase1")
                {
                    Phase1Runner phase1 = new();
                    await phase1.RunAsync(cts.Token);
                }
                else if (phase == "phase2")
                {
                    Phase2Runner phase2 = new();
                    await phase2.RunAsync(cts.Token);
                }
                else if (phase == "phase3")
                {
                    Phase3Runner phase3 = new();
                    await phase3.RunAsync(cts.Token);
                }
                else if (phase == "phase4")
                {
                    Phase4Runner phase4 = new();
                    await phase4.RunAsync(cts.Token);
                }
                else
                {
                    Console.WriteLine("Unknown phase");
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                Console.WriteLine("Run cancelled");
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
    }
}