using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase3
{
    public class PaymentGateway
    {
        JsonFileStore jsonFileStore = new();

        public async Task<PaymentResult> ChargeAsync(int orderId, decimal amount, CancellationToken ct)
        {
            Log.Info($"PaymentGateway -> Processing payment for Order #{orderId}...");


            if (orderId == 5)
            {
                await Task.Delay(5000, ct);
            }
            else
            {
                await Task.Delay(1000, ct);
            }


            if (orderId == 8)
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

                return new PaymentResult
                {
                    Success = false,
                    TransactionId = null,
                    Status = PaymentStatus.Declined,
                    Message = "Payment declined"
                };
            }

            string transactionId = Guid.NewGuid().ToString("N");

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


        public async Task RefundAsync(int orderId, string transactionId, CancellationToken ct)
        {
            Log.Info($"PaymentGateway -> Refunding payment for Order #{orderId}...");

            await Task.Delay(1000, ct);

            await jsonFileStore.UpdateAsync<PaymentTransaction>("payments.json", payments =>
                {
                    PaymentTransaction? payment = payments.FirstOrDefault(p => p.OrderId == orderId &&
                                 p.TransactionId == transactionId);

                    if (payment is null)
                    {
                        throw new InvalidOperationException($"Payment for Order #{orderId} not found");
                    }

                    payment.Status = PaymentStatus.Refunded;
                }, ct);

            Log.Success($"PaymentGateway -> Payment refunded for Order #{orderId}");
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