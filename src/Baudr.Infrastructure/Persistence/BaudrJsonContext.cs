using System.Text.Json.Serialization;
using Baudr.Core.Models;

namespace Baudr.Infrastructure.Persistence;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(ConnectionProfile))]
[JsonSerializable(typeof(SavedCommand))]
[JsonSerializable(typeof(HighlightRule))]
[JsonSerializable(typeof(SerialPortConfig))]
[JsonSerializable(typeof(PackageVerificationReport))]
[JsonSerializable(typeof(PackageVerificationErrorReport))]
public partial class BaudrJsonContext : JsonSerializerContext
{
}

