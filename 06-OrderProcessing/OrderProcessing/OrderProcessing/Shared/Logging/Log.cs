namespace OrderProcessing.Shared.Logging
{
    public static class Log
    {
        private static readonly Lock _sync = new();
        public static void Info(string message)
        {
            lock (_sync)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Thread {Thread.CurrentThread.ManagedThreadId}] {message}");
            }
        }
        public static void Success(string message)
        {
            lock (_sync)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Thread {Thread.CurrentThread.ManagedThreadId}] {message}");
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }
        public static void Error(string message)
        {
            lock (_sync)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Thread {Thread.CurrentThread.ManagedThreadId}] {message}");
                }
                finally
                {
                    Console.ResetColor();
                }
            }
        }
    }
}
