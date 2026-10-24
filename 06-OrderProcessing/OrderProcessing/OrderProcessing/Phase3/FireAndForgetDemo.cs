namespace OrderProcessing.Phase3
{
    public class FireAndForgetDemo
    {
        public async Task RunAsync()
        {
            Console.WriteLine();
            Console.WriteLine("===== FIRE AND FORGET =====");

            Console.WriteLine("Starting fire-and-forget task...");

            _ = DoWorkAsync();

            Console.WriteLine("Main method continued without waiting");

            await Task.Delay(2000);

            Console.WriteLine("Fire-and-forget demo finished");
        }

        private async Task DoWorkAsync()
        {
            await Task.Delay(1000);

            Console.WriteLine("Fire-and-forget task completed");
        }
    }
}