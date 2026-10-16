using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderProcessing.Shared.Storage
{
    public static class JsonSettings
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        static JsonSettings()
        {
            Options.Converters.Add(new JsonStringEnumConverter());
        }
    }
}
