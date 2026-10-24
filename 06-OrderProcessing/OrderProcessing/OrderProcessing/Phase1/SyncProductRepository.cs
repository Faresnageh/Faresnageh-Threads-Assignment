using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase1
{
    public class SyncProductRepository
    {
        public Product? GetByIdSync(int id)
        {
            Log.Info("SyncProductRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info($"SyncProductRepository -> Executing query for Product #{id}...");
            Thread.Sleep(Random.Shared.Next(300, 601));

            JsonFileStore jsonFileStore = new();
            List<Product> products = jsonFileStore.Read<Product>("products.json");
            foreach (var product in products)
            {
                if (product.Id == id)
                {
                    Log.Success($"SyncProductRepository -> Product #{id} found ({product.Name})");
                    return product;
                }
            }
            return null;
        }


        public bool ReserveStockSync(int productId, int qty)
        {
            Log.Info("SyncProductRepository -> Opening connection...");
            Thread.Sleep(200);

            Log.Info("SyncProductRepository -> Beginning transaction...");
            Thread.Sleep(100);


            Log.Info($"SyncProductRepository -> Updating stock for Product #{productId}...");
            Thread.Sleep(400);
            JsonFileStore jsonFileStore = new();
            bool isReserved = false;
            jsonFileStore.Update<Product>("products.json", products =>
            {
                foreach (var product in products)
                {
                    if (product.Id == productId)
                    {
                        if (product.StockQuantity >= qty)
                        {
                            product.StockQuantity -= qty;
                            Log.Success($"SyncProductRepository -> Product #{productId} reserved ({qty})");
                            isReserved = true;
                            break;
                        }
                    }
                }
            });
            Log.Info("SyncProductRepository -> Committing transaction...");
            Thread.Sleep(200);
            return isReserved;
        }
    }
}
