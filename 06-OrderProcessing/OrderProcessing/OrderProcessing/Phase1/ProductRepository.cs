using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System.Runtime.InteropServices;

namespace OrderProcessing.Phase1
{
    public class ProductRepository
    {
        public async Task<Product?> GetByIdAsync(int id, CancellationToken ct)
        {
            Log.Info("ProductRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info($"ProductRepository -> Executing query for Product #{id}...");
            await Task.Delay(Random.Shared.Next(300, 601), ct);

            JsonFileStore jsonFileStore = new();
            List<Product> products = await jsonFileStore.ReadAsync<Product>("products.json", ct);
            foreach (var product in products)
            {
                if (product.Id == id)
                {
                    Log.Success($"ProductRepository -> Product #{id} found ({product.Name})");
                    return product;
                }
            }
            return null;
        }


        public async Task<bool> ReserveStockAsync(int productId, int qty, CancellationToken ct)
        {
            Log.Info("ProductRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("ProductRepository -> Beginning transaction...");
            await Task.Delay(100, ct);


            Log.Info($"ProductRepository -> Updating stock for Product #{productId}...");
            await Task.Delay(400, ct);
            JsonFileStore jsonFileStore = new();
            bool isReserved = false;
            await jsonFileStore.UpdateAsync<Product>("products.json", products =>
            {
                foreach (var product in products)
                {
                    if (product.Id == productId)
                    {
                        if (product.StockQuantity >= qty)
                        {
                            product.StockQuantity -= qty;
                            Log.Success($"ProductRepository -> Product #{productId} reserved ({qty})");
                            isReserved = true;
                            break;
                        }
                    }
                }
            }, ct);
            Log.Info("ProductRepository -> Committing transaction...");
            try
            {
                await Task.Delay(200, ct);
            }
            catch (OperationCanceledException) when (isReserved && ct.IsCancellationRequested)
            {
                
            }

            return isReserved;
        }



        public async Task ReleaseStockAsync(int productId, int qty, CancellationToken ct)
        {
            JsonFileStore jsonFileStore = new();
            Log.Info("ProductRepository -> Opening connection...");
            await Task.Delay(200, ct);
            Log.Info("ProductRepository -> Beginning transaction...");
            await Task.Delay(100, ct);
            Log.Info($"ProductRepository -> Releasing stock for Product #{productId}...");
            await Task.Delay(400, ct);
            await jsonFileStore.UpdateAsync<Product>("products.json", products =>
                {
                    Product? product = products.FirstOrDefault(p => p.Id == productId);

                    if (product is null)
                    {
                        throw new InvalidOperationException($"Product #{productId} not found");
                    }

                    product.StockQuantity += qty;
                }, ct);

            Log.Info("ProductRepository -> Committing transaction...");
            await Task.Delay(200, ct);
        }


        public async Task<bool> ReserveStockUnsafeAsync(int productId, int qty, CancellationToken ct)
        {
            JsonFileStore jsonFileStore = new();

            List<Product> products = await jsonFileStore.ReadAsync<Product>("products.json", ct);

            Product? product = products.FirstOrDefault(p => p.Id == productId);

            if (product is null || product.StockQuantity < qty)
            {
                return false;
            }

            Log.Info("ProductRepository -> Opening connection...");
            await Task.Delay(200, ct);

            Log.Info("ProductRepository -> Beginning transaction...");
            await Task.Delay(100, ct);

            Log.Info($"ProductRepository -> Updating stock for Product #{productId}...");
            await Task.Delay(400, ct);

            product.StockQuantity -= qty;

            await jsonFileStore.WriteAsync("products.json", products, ct);

            Log.Info("ProductRepository -> Committing transaction...");
            await Task.Delay(200, ct);

            return true;
        }

    }
}
