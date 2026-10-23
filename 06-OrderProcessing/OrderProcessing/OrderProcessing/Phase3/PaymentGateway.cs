using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3
{
    public class PaymentGateway : IPaymentGateway
    {
        JsonFileStore jsonFileStore = new();

        HashSet<int> timeoutOrderIds;
        HashSet<int> declineOrderIds;

        public PaymentGateway(IEnumerable<int>? timeoutOrderIds = null, IEnumerable<int>? declineOrderIds = null)
        {
            this.timeoutOrderIds = timeoutOrderIds?.ToHashSet() ?? new();
            this.declineOrderIds = declineOrderIds?.ToHashSet() ?? new();
        }

        public async Task<PaymentResult> ProcessPaymentAsync(int orderId, decimal amount, CancellationToken ct)
        {
            Log.Info($"PaymentGateway -> Authorizing payment for Order #{orderId}...");
            await Task.Delay(800, ct);

            Log.Info($"PaymentGateway -> Fraud check for Order #{orderId}...");
            await Task.Delay(500, ct);

            if (declineOrderIds.Contains(orderId))
            {
                PaymentTransaction declinedTransaction = new()
                {
                    OrderId = orderId,
                    Amount = amount,
                    Status = PaymentStatus.Declined,
                    TransactionId = null,
                    CreatedAt = DateTime.Now
                };

                await SaveTransactionAsync(declinedTransaction, ct);

                Log.Error($"PaymentGateway -> Payment declined for Order #{orderId}");

                return new PaymentResult
                {
                    Success = false,
                    TransactionId = null,
                    Status = PaymentStatus.Declined,
                    Message = "Card declined by issuer"
                };
            }

            Log.Info($"PaymentGateway -> Capturing payment for Order #{orderId}...");

            if (timeoutOrderIds.Contains(orderId))
            {
                await Task.Delay(5000, ct);
            }
            else
            {
                await Task.Delay(700, ct);
            }

            string transactionId = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpper();

            PaymentTransaction transaction = new()
            {
                OrderId = orderId,
                Amount = amount,
                Status = PaymentStatus.Captured,
                TransactionId = transactionId,
                CreatedAt = DateTime.Now
            };

            await SaveTransactionAsync(transaction, ct);

            Log.Success($"PaymentGateway -> Payment captured for Order #{orderId}");

            return new PaymentResult
            {
                Success = true,
                TransactionId = transactionId,
                Status = PaymentStatus.Captured,
                Message = "Payment captured"
            };
        }

        public async Task RefundAsync(string transactionId, CancellationToken ct)
        {
            Log.Info($"PaymentGateway -> Refunding payment {transactionId}...");

            await Task.Delay(150, ct);
            await Task.Delay(300, ct);
            await Task.Delay(Random.Shared.Next(500, 1501), ct);

            await jsonFileStore.UpdateAsync<PaymentTransaction>("payments.json", payments =>
            {
                PaymentTransaction? payment = payments.FirstOrDefault(p => p.TransactionId == transactionId);

                if (payment is null)
                {
                    throw new InvalidOperationException($"Payment {transactionId} not found");
                }

                payment.Status = PaymentStatus.Refunded;
            }, ct);

            Log.Success($"PaymentGateway -> Payment {transactionId} refunded");
        }

        private async Task SaveTransactionAsync(PaymentTransaction transaction, CancellationToken ct)
        {
            await jsonFileStore.UpdateAsync<PaymentTransaction>("payments.json", payments =>
            {
                payments.Add(transaction);
            }, ct);
        }
    }
}