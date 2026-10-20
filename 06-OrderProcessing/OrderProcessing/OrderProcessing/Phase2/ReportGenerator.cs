
using System.Text.Json;
using OrderProcessing.Phase1;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2
{
    public class ReportGenerator
    {
        private readonly OrderRepository _orderRepository;
        private readonly ProductRepository _productRepository;

        public ReportGenerator(OrderRepository orderRepository, ProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        public async Task<Report> GenerateAsync(CancellationToken ct)
        {
            Log.Info("ReportGenerator -> Loading orders...");

            List<Order> orders = await _orderRepository.GetAllAsync(ct);

            List<Order> completedOrders = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

            decimal totalRevenue = Math.Round(completedOrders.Sum(o => o.TotalAmount), 2, MidpointRounding.AwayFromZero);

            var topProduct = completedOrders.SelectMany(o => o.Items).GroupBy(item => item.ProductId).Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            }).OrderByDescending(x => x.Quantity).ThenBy(x => x.ProductId).FirstOrDefault();

            string topProductName = "";
            int topSellingQuantity = 0;

            if (topProduct != null)
            {
                Product? product = await _productRepository.GetByIdAsync(topProduct.ProductId, ct);

                topProductName = product?.Name ?? $"Product #{topProduct.ProductId}";

                topSellingQuantity = topProduct.Quantity;
            }

            Report report = new Report
            {
                TotalOrders = orders.Count,
                CompletedCount = completedOrders.Count,
                RejectedCount = orders.Count(o => o.Status == OrderStatus.Rejected),
                FailedCount = orders.Count(o => o.Status == OrderStatus.Failed),
                PaymentFailedCount = orders.Count(o => o.Status == OrderStatus.PaymentFailed),
                TotalRevenue = totalRevenue,
                AverageOrderValue = completedOrders.Count == 0 ? 0m : Math.Round(totalRevenue / completedOrders.Count, 2, MidpointRounding.AwayFromZero),
                TopSellingProduct = topProductName,
                TopSellingQuantity = topSellingQuantity,
                GeneratedAt = DateTime.Now
            };

            string directory = Path.Combine(AppContext.BaseDirectory, "Data", "reports");
            Directory.CreateDirectory(directory);
            string fileName = $"report_{report.GeneratedAt:yyyyMMdd_HHmmss}.json";
            string filePath = Path.Combine(directory, fileName);
            string json = JsonSerializer.Serialize(report, JsonSettings.Options);

            await File.WriteAllTextAsync(filePath, json, ct);
            Log.Success($"ReportGenerator -> Report saved: {filePath}");

            return report;
        }
    }
}
