using System.Text.Json.Serialization;
using Opure.Runtime.Contracts.Plugins;

namespace Opure.Runtime.Plugins;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(PluginManifest))]
internal partial class RuntimePluginSerializationContext : JsonSerializerContext
{
}
