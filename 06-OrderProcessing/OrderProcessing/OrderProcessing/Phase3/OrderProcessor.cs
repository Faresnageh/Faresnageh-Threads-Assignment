using OrderProcessing.Exceptions;
using OrderProcessing.Phase1;
using OrderProcessing.Phase2;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;

namespace OrderProcessing.Phase3
{
    public class OrderProcessor
    {
        ProductRepository productRepository;
        OrderRepository orderRepository;
        CustomerRepository customerRepository;
        OrderPricer orderPricer;
        IPaymentGateway paymentGateway;
        IEmailService emailService;

        public OrderProcessor(ProductRepository productRepository, OrderRepository orderRepository, CustomerRepository customerRepository, OrderPricer orderPricer, IPaymentGateway paymentGateway, IEmailService emailService)
        {
            this.productRepository = productRepository;
            this.orderRepository = orderRepository;
            this.customerRepository = customerRepository;
            this.orderPricer = orderPricer;
            this.paymentGateway = paymentGateway;
            this.emailService = emailService;
        }

        public async Task<bool> ProcessAsync(Order order, CancellationToken ct)
        {
            List<OrderItem> reservedItems = new();
            PaymentResult? paymentResult = null;
            bool completed = false;

            try
            {
                foreach (OrderItem item in order.Items)
                {
                    bool reserved = await productRepository.ReserveStockAsync(item.ProductId, item.Quantity, ct);

                    if (!reserved)
                    {
                        Product? product = await productRepository.GetByIdAsync(item.ProductId, ct);

                        string productName = product?.Name ?? $"Product #{item.ProductId}";

                        order.Status = OrderStatus.Rejected;
                        order.FailureReason = $"Insufficient stock for '{productName}'";

                        await orderRepository.SaveAsync(order, ct);

                        return false;
                    }

                    reservedItems.Add(item);
                }

                await orderPricer.PriceOrderAsync(order, ct);

                using CancellationTokenSource paymentCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

                paymentCts.CancelAfter(3000);

                try
                {
                    paymentResult = await paymentGateway.ProcessPaymentAsync(order.Id, order.TotalAmount, paymentCts.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    order.Status = OrderStatus.PaymentFailed;
                    order.FailureReason = "Payment gateway timeout (3s)";

                    await orderRepository.SaveAsync(order, ct);

                    return false;
                }

                if (!paymentResult.Success)
                {
                    order.Status = OrderStatus.PaymentFailed;
                    order.FailureReason = paymentResult.Message;

                    await orderRepository.SaveAsync(order, ct);

                    return false;
                }

                order.TransactionId = paymentResult.TransactionId;
                order.Status = OrderStatus.Completed;
                order.FailureReason = null;

                await orderRepository.SaveAsync(order, ct);

                Customer? customer = await customerRepository.GetByIdAsync(order.CustomerId, ct);

                if (customer is not null)
                {
                    await SendEmailWithRetryAsync(order, customer, ct);
                }

                completed = true;

                return true;
            }
            catch (DatabaseWriteException)
            {
                if (paymentResult?.Success == true && paymentResult.TransactionId is not null)
                {
                    await paymentGateway.RefundAsync(paymentResult.TransactionId, CancellationToken.None);
                }

                order.Status = OrderStatus.Failed;
                order.FailureReason = $"Database save failed after payment; payment {paymentResult?.TransactionId} refunded";

                await orderRepository.SaveAsync(order, CancellationToken.None);

                return false;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                if (paymentResult?.Success == true && paymentResult.TransactionId is not null)
                {
                    await paymentGateway.RefundAsync(paymentResult.TransactionId, CancellationToken.None);
                }

                order.Status = OrderStatus.Pending;
                order.FailureReason = "Cancelled";

                await orderRepository.SaveAsync(order, CancellationToken.None);

                throw;
            }
            catch (Exception ex)
            {
                if (paymentResult?.Success == true && paymentResult.TransactionId is not null)
                {
                    await paymentGateway.RefundAsync(paymentResult.TransactionId, CancellationToken.None);
                }

                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;

                await orderRepository.SaveAsync(order, CancellationToken.None);

                return false;
            }
            finally
            {
                if (!completed)
                {
                    foreach (OrderItem item in reservedItems)
                    {
                        await productRepository.ReleaseStockAsync(item.ProductId, item.Quantity, CancellationToken.None);
                    }
                }
            }
        }

        private async Task SendEmailWithRetryAsync(Order order, Customer customer, CancellationToken ct)
        {
            const int maxAttempts = 4;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (attempt == 2)
                {
                    await Task.Delay(500, ct);
                }
                else if (attempt == 3)
                {
                    await Task.Delay(1000, ct);
                }
                else if (attempt == 4)
                {
                    await Task.Delay(2000, ct);
                }

                try
                {
                    await emailService.SendConfirmationAsync(order, customer, ct);
                    return;
                }
                catch (SmtpUnavailableException ex)
                {
                    Log.Info($"EmailService -> Attempt {attempt} failed for Order #{order.Id}: {ex.Message}");

                    if (attempt == maxAttempts)
                    {
                        await emailService.AddToRetryAsync(order, customer, ct);
                        return;
                    }
                }
            }
        }
    }
}