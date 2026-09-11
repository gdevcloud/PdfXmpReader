using System.Text;
using System.Text.Json;

namespace PdfXmpReader;

internal static class QpdfMetadataReader
{
    public static string? Read(string inputFile)
    {
        var outputFile = Path.Combine(Path.GetTempPath(), $"pdf-xmp-reader-{Guid.NewGuid():N}.json");
        try
        {
            var job = new
            {
                inputFile,
                json = "latest",
                jsonStreamData = "inline",
                decodeLevel = "generalized",
                outputFile
            };

            QpdfNativeApi.ExecuteJob(JsonSerializer.Serialize(job));
            using var document = JsonDocument.Parse(File.ReadAllBytes(outputFile));
            var qpdf = document.RootElement.GetProperty("qpdf");
            var objects = qpdf[1];
            var trailer = objects.GetProperty("trailer").GetProperty("value");
            var catalog = Resolve(trailer.GetProperty("/Root"), objects);
            if (catalog.ValueKind != JsonValueKind.Object
                || !catalog.TryGetProperty("/Metadata", out var metadataReference))
            {
                return null;
            }

            var metadata = Resolve(metadataReference, objects);
            if (metadata.ValueKind != JsonValueKind.Object
                || !metadata.TryGetProperty("data", out var data)
                || data.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            return Encoding.UTF8.GetString(data.GetBytesFromBase64());
        }
        finally
        {
            File.Delete(outputFile);
        }
    }

    private static JsonElement Resolve(JsonElement value, JsonElement objects)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (value.ValueKind == JsonValueKind.String)
        {
            var reference = value.GetString();
            if (reference is null
                || !reference.EndsWith(" R", StringComparison.Ordinal)
                || !visited.Add(reference))
            {
                return value;
            }

            if (!objects.TryGetProperty($"obj:{reference}", out var target))
                return value;

            if (target.TryGetProperty("value", out var resolvedValue))
            {
                value = resolvedValue;
                continue;
            }

            if (target.TryGetProperty("stream", out var stream))
                return stream;

            return value;
        }

        return value;
    }
}
