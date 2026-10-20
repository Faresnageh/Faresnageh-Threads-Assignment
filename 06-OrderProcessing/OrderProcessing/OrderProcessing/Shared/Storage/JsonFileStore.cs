using OrderProcessing.Exceptions;
using System.Text.Json;

namespace OrderProcessing.Shared.Storage
{
    public class JsonFileStore
    {
        private readonly string _dataPath = Path.Combine(AppContext.BaseDirectory, "Data");
        public async Task<List<T>> ReadAsync<T>(string fileName, CancellationToken ct = default)
        {
            string fullPath = Path.Combine(_dataPath, fileName);

            if (File.Exists(fullPath) == false)
                return new List<T>();

            string json = await File.ReadAllTextAsync(fullPath, ct);

            try
            {
                List<T>? data = JsonSerializer.Deserialize<List<T>>(json, JsonSettings.Options);
                if (data is null)
                    return new List<T>();
                return data;
            }
            catch (JsonException)
            {
                throw new DataFileCorruptedException(fileName);
            }
        }
        public List<T> Read<T>(string fileName)
        {
            string fullPath = Path.Combine(_dataPath, fileName);

            if (File.Exists(fullPath) == false)
                return new List<T>();

            string json =  File.ReadAllText(fullPath);

            try
            {
                List<T>? data = JsonSerializer.Deserialize<List<T>>(json, JsonSettings.Options);
                if (data is null)
                    return new List<T>();
                return data;
            }
            catch (JsonException)
            {
                throw new DataFileCorruptedException(fileName);
            }
        }
        private static readonly SemaphoreSlim semaphoreSlim = new(1, 1);

        public async Task UpdateAsync<T>(string fileName, Action<List<T>> update, CancellationToken ct = default)
        {
            await semaphoreSlim.WaitAsync(ct);
            try
            {
                List<T> data = await ReadAsync<T>(fileName, ct);
                update(data);
                await WriteCoreAsync(fileName, data, ct);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
        public void Update<T>(string fileName, Action<List<T>> update, CancellationToken ct = default)
        {
             semaphoreSlim.Wait(ct);
            try
            {
                List<T> data =  Read<T>(fileName);
                update(data);
                WriteCore(fileName, data, ct);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        private async Task WriteCoreAsync<T>(string fileName, List<T> data, CancellationToken ct = default)
        {
            string fullPath = Path.Combine(_dataPath, fileName);
            string tmpPath = fullPath + ".tmp";
            try
            {
                Directory.CreateDirectory(_dataPath);
                string json = JsonSerializer.Serialize(data, JsonSettings.Options);
                await File.WriteAllTextAsync(tmpPath, json, ct);
                ct.ThrowIfCancellationRequested();
                File.Move(tmpPath, fullPath, true);
            }
            finally
            {
                File.Delete(tmpPath);
            }
        }
        private void WriteCore<T>(string fileName, List<T> data, CancellationToken ct = default)
        {
            string fullPath = Path.Combine(_dataPath, fileName);
            string tmpPath = fullPath + ".tmp";
            try
            {
                Directory.CreateDirectory(_dataPath);
                string json = JsonSerializer.Serialize(data, JsonSettings.Options);
                File.WriteAllText(tmpPath, json);
                File.Move(tmpPath, fullPath, true);
            }
            finally
            {
                File.Delete(tmpPath);
            }
        }


        public async Task WriteAsync<T>(string fileName, List<T> data, CancellationToken ct = default)
        {
            await semaphoreSlim.WaitAsync(ct);
            try
            {
                await WriteCoreAsync(fileName, data, ct);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
        public void Write<T>(string fileName, List<T> data, CancellationToken ct = default)
        {
             semaphoreSlim.Wait(ct);
            try
            {
                 WriteCore(fileName, data, ct);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}
