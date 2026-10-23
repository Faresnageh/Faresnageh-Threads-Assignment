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
        PaymentGateway paymentGateway;
        EmailService emailService;

        public OrderProcessor(ProductRepository productRepository,OrderRepository orderRepository,CustomerRepository customerRepository,
            OrderPricer orderPricer,
            PaymentGateway paymentGateway,
            EmailService emailService)
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
                    bool reserved = await productRepository.ReserveStockAsync( item.ProductId,item.Quantity,ct);

                    if (!reserved)
                    {
                        Product? product = await productRepository.GetByIdAsync(item.ProductId,ct);

                        string productName = product?.Name ?? $"Product #{item.ProductId}";

                        order.Status = OrderStatus.Rejected;
                        order.FailureReason = $"Insufficient stock for '{productName}'";

                        await orderRepository.SaveAsync(order, ct);

                        return false;
                    }

                    reservedItems.Add(item);
                }


                await orderPricer.PriceOrderAsync(order, ct);


                using CancellationTokenSource paymentCts =CancellationTokenSource.CreateLinkedTokenSource(ct);

                paymentCts.CancelAfter(3000);

                try
                {
                    paymentResult = await paymentGateway.ChargeAsync(order.Id,order.TotalAmount,paymentCts.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    order.Status = OrderStatus.PaymentFailed;
                    order.FailureReason = "Payment timeout";

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


                if (order.Id == 13)
                {
                    throw new DatabaseWriteException("Database write failed after payment");
                }


                order.Status = OrderStatus.Completed;
                order.FailureReason = null;

                await orderRepository.SaveAsync(order, ct);

                completed = true;


                Customer? customer = await customerRepository.GetByIdAsync(order.CustomerId,ct);

                if (customer is not null)
                {
                    EmailMessage email = new()
                    {
                        OrderId = order.Id,
                        To = customer.Email,
                        Subject = $"Order #{order.Id} Confirmation",
                        Body = $"Your order #{order.Id} has been completed. Total: {order.TotalAmount:F2}",
                        Attempts = 0
                    };

                    await SendEmailWithRetryAsync(email, ct);
                }

                return true;
            }
            catch (DatabaseWriteException ex)
            {
                if (paymentResult?.Success == true &&
                    paymentResult.TransactionId is not null)
                {
                    await paymentGateway.RefundAsync(order.Id,paymentResult.TransactionId,CancellationToken.None);
                }

                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;

                await orderRepository.SaveAsync(order,CancellationToken.None);

                return false;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                if (paymentResult?.Success == true &&paymentResult.TransactionId is not null)
                {
                    await paymentGateway.RefundAsync(order.Id,paymentResult.TransactionId,CancellationToken.None);
                }

                order.Status = OrderStatus.Pending;
                order.FailureReason = "Cancelled";

                await orderRepository.SaveAsync(order,CancellationToken.None);

                throw;
            }
            catch (Exception ex)
            {
                if (paymentResult?.Success == true &&paymentResult.TransactionId is not null)
                {
                    await paymentGateway.RefundAsync(order.Id,paymentResult.TransactionId,CancellationToken.None);
                }

                order.Status = OrderStatus.Failed;
                order.FailureReason = ex.Message;

                await orderRepository.SaveAsync(order,CancellationToken.None);

                return false;
            }
            finally
            {
                if (!completed)
                {
                    foreach (OrderItem item in reservedItems)
                    {
                        await productRepository.ReleaseStockAsync(item.ProductId,item.Quantity, CancellationToken.None);
                    }
                }
            }
        }


        private async Task SendEmailWithRetryAsync(EmailMessage email,CancellationToken ct)
        {
            const int maxAttempts = 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await emailService.SendAsync(email, ct);
                    return;
                }
                catch (Exception ex) when (ex is not OperationCanceledException ||!ct.IsCancellationRequested)
                {
                    email.LastError = ex.Message;

                    Log.Info($"EmailService -> Attempt {attempt} failed for Order #{email.OrderId}");

                    if (attempt == maxAttempts)
                    {
                        await emailService.AddToRetryAsync(email, ct);
                        return;
                    }

                    await Task.Delay(500, ct);
                }
            }
        }
    }
}