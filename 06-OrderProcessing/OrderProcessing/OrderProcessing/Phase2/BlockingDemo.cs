namespace OrderProcessing.Phase2
{
    public static class BlockingDemo
    {
        public static void Run()
        {
            Console.WriteLine("===== BLOCKING DEMO =====");

            Task task = Task.Delay(1000);

            Console.WriteLine("Before Wait");
            task.Wait();
            Console.WriteLine("After Wait");

            Task<int> numberTask = GetNumberAsync(CancellationToken.None);

            Console.WriteLine("Before Result");
            int result = numberTask.Result;
            Console.WriteLine($"Result: {result}");
        }

        private static async Task<int> GetNumberAsync(CancellationToken ct)
        {
            await Task.Delay(1000, ct);
            return 42;
        }
    }
}
