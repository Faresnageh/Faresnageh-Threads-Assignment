using OrderProcessing.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase4
{
    public class CustomerDashboardService
    {
        JsonFileStore jsonFileStore = new();

        HashSet<int> failPaymentsForCustomerIds;

        public CustomerDashboardService(IEnumerable<int>? failPaymentsForCustomerIds = null)
        {
            this.failPaymentsForCustomerIds = failPaymentsForCustomerIds?.ToHashSet() ?? new();
        }

        public async Task<CustomerDashboard> LoadAsync(int customerId, CancellationToken ct)
        {
            Task<Customer> customerTask = LoadCustomerAsync(customerId, ct);
            Task<List<Order>> ordersTask = LoadOrdersAsync(customerId, ct);
            Task<List<PaymentTransaction>> paymentsTask = LoadPaymentsAsync(customerId, ct);
            Task<List<EmailMessage>> emailsTask = LoadEmailsAsync(customerId, ct);

            Task[] allTasks =
            [
                customerTask,
                ordersTask,
                paymentsTask,
                emailsTask
            ];

            try
            {
                await Task.WhenAll(allTasks);
            }
            catch
            {
                List<Exception> errors = new();

                foreach (Task task in allTasks)
                {
                    if (task.Exception is not null)
                    {
                        errors.AddRange(task.Exception.InnerExceptions);
                    }
                }

                throw new AggregateException(errors);
            }

            return new CustomerDashboard
            {
                Customer = customerTask.Result,
                Orders = ordersTask.Result,
                Payments = paymentsTask.Result,
                Emails = emailsTask.Result
            };
        }

        private async Task<Customer> LoadCustomerAsync(int customerId, CancellationToken ct)
        {
            Log.Info($"CustomerDashboard -> Opening customer connection...");
            await Task.Delay(200, ct);

            Log.Info($"CustomerDashboard -> Loading Customer #{customerId}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Customer> customers = await jsonFileStore.ReadAsync<Customer>("customers.json", ct);

            Customer? customer = customers.FirstOrDefault(c => c.Id == customerId);

            if (customer is null)
            {
                throw new CustomerNotFoundException($"Customer #{customerId} not found");
            }

            return customer;
        }

        private async Task<List<Order>> LoadOrdersAsync(int customerId, CancellationToken ct)
        {
            Log.Info($"CustomerDashboard -> Opening orders connection...");
            await Task.Delay(200, ct);

            Log.Info($"CustomerDashboard -> Loading orders for Customer #{customerId}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Order> orders = await jsonFileStore.ReadAsync<Order>("orders.json", ct);

            return orders.Where(o => o.CustomerId == customerId).ToList();
        }

        private async Task<List<PaymentTransaction>> LoadPaymentsAsync(int customerId, CancellationToken ct)
        {
            Log.Info($"CustomerDashboard -> Opening payments connection...");
            await Task.Delay(200, ct);

            Log.Info($"CustomerDashboard -> Loading payments for Customer #{customerId}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            if (failPaymentsForCustomerIds.Contains(customerId))
            {
                throw new TimeoutException("Payments store timeout");
            }

            List<Order> orders = await jsonFileStore.ReadAsync<Order>("orders.json", ct);

            List<int> orderIds = orders.Where(o => o.CustomerId == customerId).Select(o => o.Id)
                .ToList();

            List<PaymentTransaction> payments = await jsonFileStore.ReadAsync<PaymentTransaction>("payments.json", ct);

            return payments.Where(p => orderIds.Contains(p.OrderId)).ToList();
        }

        private async Task<List<EmailMessage>> LoadEmailsAsync(int customerId, CancellationToken ct)
        {
            Log.Info($"CustomerDashboard -> Opening emails connection...");
            await Task.Delay(200, ct);

            Log.Info($"CustomerDashboard -> Loading emails for Customer #{customerId}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            List<Order> orders = await jsonFileStore.ReadAsync<Order>("orders.json", ct);

            List<int> orderIds = orders.Where(o => o.CustomerId == customerId).Select(o => o.Id)
                .ToList();

            List<EmailMessage> emails = await jsonFileStore.ReadAsync<EmailMessage>("emails.json", ct);

            return emails.Where(e => orderIds.Contains(e.OrderId)).ToList();
        }
    }
}