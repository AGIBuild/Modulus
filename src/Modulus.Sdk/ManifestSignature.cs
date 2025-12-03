using System.Text.Json.Serialization;

namespace Modulus.Sdk;

public sealed class ManifestSignature
{
    [JsonPropertyName("algorithm")]
    public string Algorithm { get; init; } = "SHA256";

    [JsonPropertyName("file")]
    public required string File { get; init; }
}

