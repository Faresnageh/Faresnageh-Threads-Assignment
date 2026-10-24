using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase4
{
    public class LiveCheckoutResult
    {
        public Order Order { get; set; } = new();
        public bool Success { get; set; }
        public double ElapsedSeconds { get; set; }
    }
}