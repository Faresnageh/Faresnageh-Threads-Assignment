using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1
{
    public class SyncCustomerRepository
    {
        public Customer? GetByIdSync(int id)
        {
            Log.Info("SyncCustomerRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info($"SyncCustomerRepository -> Executing query for Customer #{id}...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            JsonFileStore jsonFileStore = new();
            List<Customer> customers = jsonFileStore.Read<Customer>("customers.json");
            foreach (var customer in customers)
            {
                if (customer.Id == id)
                {
                    Log.Success($"SyncCustomerRepository -> Customer #{id} found ({customer.Name})");
                    return customer;
                }
            }
            return null;
        }
    }
}
