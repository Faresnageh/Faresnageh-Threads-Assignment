using System.Diagnostics;

namespace OrderProcessing.Phase3
{
    public class SyncVsAsyncDemo
    {
        public async Task RunAsync()
        {
            Console.WriteLine();
            Console.WriteLine("===== SYNC VS ASYNC =====");

            RunWithThreads();

            await RunWithAsyncTasks();
        }

        private void RunWithThreads()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<Thread> threads = new();

            for (int i = 1; i <= 10; i++)
            {
                int taskNumber = i;

                Thread thread = new(() =>
                {
                    Console.WriteLine($"Thread #{taskNumber} started on Thread {Environment.CurrentManagedThreadId}");

                    Thread.Sleep(1000);

                    Console.WriteLine($"Thread #{taskNumber} finished");
                });

                threads.Add(thread);
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            stopwatch.Stop();

            Console.WriteLine($"10 Threads Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
        }

        private async Task RunWithAsyncTasks()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<Task> tasks = new();

            for (int i = 1; i <= 10; i++)
            {
                int taskNumber = i;

                Task task = RunAsyncOperation(taskNumber);

                tasks.Add(task);
            }

            foreach (Task task in tasks)
            {
                await task;
            }

            stopwatch.Stop();

            Console.WriteLine($"10 Async Tasks Time: {stopwatch.Elapsed.TotalSeconds:F2}s");
        }

        private async Task RunAsyncOperation(int taskNumber)
        {
            Console.WriteLine($"Async Task #{taskNumber} started on Thread {Environment.CurrentManagedThreadId}");

            await Task.Delay(1000);

            Console.WriteLine($"Async Task #{taskNumber} finished on Thread {Environment.CurrentManagedThreadId}");
        }
    }
}