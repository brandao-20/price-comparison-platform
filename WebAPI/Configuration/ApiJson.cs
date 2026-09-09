using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using WebAPI.Entities;

namespace WebAPI.Configuration;

public static class ApiJson
{
    public static void Configure(JsonSerializerOptions options)
    {
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                typeInfo =>
                {
                    if (typeInfo.Type != typeof(Utilizador)) return;
                    foreach (var property in typeInfo.Properties)
                    {
                        if (property.Name.Equals("Password", StringComparison.OrdinalIgnoreCase)
                            || property.Name.Equals("GoogleToken", StringComparison.OrdinalIgnoreCase)
                            || property.Name.Equals("GoogleId", StringComparison.OrdinalIgnoreCase))
                            // Keep existing request binding while excluding credentials from every response, including nested entities.
                            property.ShouldSerialize = (_, _) => false;
                    }
                }
            }
        };
    }
}
