using System.Text.Json.Serialization;
using SharpLoadTester;
using static SharpLoadTester.Stats;
[JsonSerializable(typeof(Stats))]
[JsonSerializable(typeof(RpsStatistics))] // Add any other types used in serialization
public partial class MyJsonContext : JsonSerializerContext
{
}
