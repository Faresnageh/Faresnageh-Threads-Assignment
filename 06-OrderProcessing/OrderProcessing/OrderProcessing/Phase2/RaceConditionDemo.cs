using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;

namespace OrderProcessing.Phase2
{
    public class RaceConditionDemo
    {
        public async Task RunAsync(CancellationToken ct)
        {
            JsonFileStore jsonFileStore = new();
            ProductRepository productRepository = new();

            await jsonFileStore.UpdateAsync<Product>("products.json", products =>
                {
                    Product? keyboard = products.FirstOrDefault(p => p.Id == 4);

                    if (keyboard is null)
                        throw new InvalidOperationException("Keyboard not found");

                    keyboard.StockQuantity = 1;
                }, ct);

            Console.WriteLine("===== Unsafe Reservation =====");

            Task<bool> order10Task = productRepository.ReserveStockUnsafeAsync(4, 1, ct);

            Task<bool> order11Task = productRepository.ReserveStockUnsafeAsync(4, 1, ct);

            await Task.WhenAll(order10Task, order11Task);

            List<Product> productsAfterUnsafe = await jsonFileStore.ReadAsync<Product>("products.json", ct);

            Product keyboardAfterUnsafe = productsAfterUnsafe.First(p => p.Id == 4);

            Console.WriteLine($"Order #10: {await order10Task}");
            Console.WriteLine($"Order #11: {await order11Task}");
            Console.WriteLine($"Final Stock: {keyboardAfterUnsafe.StockQuantity}");


            await jsonFileStore.UpdateAsync<Product>("products.json", products =>
                {
                    Product? keyboard =
                        products.FirstOrDefault(p => p.Id == 4);

                    if (keyboard is null)
                        throw new InvalidOperationException("Keyboard not found");

                    keyboard.StockQuantity = 1;
                }, ct); ;

            Console.WriteLine();
            Console.WriteLine("===== Safe Reservation =====");

            Task<bool> safeOrder10Task = productRepository.ReserveStockAsync(4, 1, ct);

            Task<bool> safeOrder11Task = productRepository.ReserveStockAsync(4, 1, ct);

            await Task.WhenAll(safeOrder10Task, safeOrder11Task);

            List<Product> productsAfterSafe = await jsonFileStore.ReadAsync<Product>("products.json", ct);

            Product keyboardAfterSafe = productsAfterSafe.First(p => p.Id == 4);

            Console.WriteLine($"Order #10: {await safeOrder10Task}");
            Console.WriteLine($"Order #11: {await safeOrder11Task}");
            Console.WriteLine($"Final Stock: {keyboardAfterSafe.StockQuantity}");
        }
    }
}
