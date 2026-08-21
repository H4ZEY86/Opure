using System.Collections.Generic;
using System.Text.Json.Serialization;
using Opure.Runtime.Contracts.Plugins;

namespace Opure.Runtime.Sqlite;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(PluginManifest))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(string[]))]
internal partial class PluginSerializationContext : JsonSerializerContext
{
}
