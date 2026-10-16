using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Phase1Runner phase1 = new();
            await phase1.RunAsync(default);
        }
    }
}
