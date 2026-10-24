using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1
{
    public class CustomerRepository
    {
        public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
        {
            Log.Info("CustomerRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"CustomerRepository -> Executing query for Customer #{id}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            JsonFileStore jsonFileStore = new();
            List<Customer> customers = await jsonFileStore.ReadAsync<Customer>("customers.json", ct);
            foreach (var customer in customers)
            {
                if (customer.Id == id)
                {
                    Log.Success($"CustomerRepository -> Customer #{id} found ({customer.Name})");
                    return customer;
                }
            }
            return null;
        }
    }
}
