using System.Text.Json;
using System.Threading.Tasks;
using Modulus.Sdk;

namespace Modulus.Core.Manifest;

public static class ManifestReader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true
    };

    public static async Task<ModuleManifest?> ReadAsync(Stream stream)
    {
        return await JsonSerializer.DeserializeAsync<ModuleManifest>(stream, Options).ConfigureAwait(false);
    }

    public static async Task<ModuleManifest?> ReadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await ReadAsync(stream).ConfigureAwait(false);
    }
}

