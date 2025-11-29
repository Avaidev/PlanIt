using System.Text.Json.Serialization;
using PlanIt.Data.Models;

namespace PlanIt.Core.Services;
[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, 
    WriteIndented = true)]
public partial class AppJsonContext : JsonSerializerContext
{ }