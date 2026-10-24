using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase2
{
    public class OrderProcessorV2
    {
        private readonly ProductRepository _productRepository;
        private readonly OrderRepository _orderRepository;
        private readonly OrderPricer _orderPricer;
        private readonly InvoiceGenerator _invoiceGenerator;

        public OrderProcessorV2(ProductRepository productRepository, OrderRepository orderRepository, OrderPricer orderPricer, InvoiceGenerator invoiceGenerator)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _orderPricer = orderPricer;
            _invoiceGenerator = invoiceGenerator;
        }


        public async Task<bool> ProcessValidatedOrderAsync(Order order, CancellationToken ct)
        {
            List<OrderItem> reservedItems = new();
            bool completed = false;

            try
            {
                foreach (OrderItem item in order.Items)
                {
                    bool reserved = await _productRepository.ReserveStockAsync(item.ProductId, item.Quantity, ct);

                    if (!reserved)
                    {
                        Product? product = await _productRepository.GetByIdAsync(item.ProductId, ct);

                        string productName = product?.Name ?? $"Product #{item.ProductId}";

                        order.Status = OrderStatus.Rejected;
                        order.FailureReason = $"Insufficient stock for '{productName}'";

                        await _orderRepository.SaveAsync(order, ct);
                        return false;
                    }

                    reservedItems.Add(item);
                }

                await _orderPricer.PriceOrderAsync(order, ct);
                await _invoiceGenerator.GenerateAsync(order, ct);

                order.Status = OrderStatus.Completed;
                await _orderRepository.SaveAsync(order, ct);

                completed = true;
                return true;
            }
            finally
            {
                if (!completed)
                {
                    foreach (OrderItem reservedItem in reservedItems)
                    {
                        await _productRepository.ReleaseStockAsync(
                            reservedItem.ProductId,
                            reservedItem.Quantity,
                            CancellationToken.None);
                    }
                }
            }
        }

    }
}
