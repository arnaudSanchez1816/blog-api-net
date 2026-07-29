using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BlogApi.Transformers;

/// <summary>
/// Schema Transformer that actually populate open api document schema from a AllowedValues attribute (this does not work
/// by default)
/// </summary>
public class AllowedValuesSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Query/route/header-bound parameters carry their DataAnnotations attributes on
        // ParameterDescription.ModelMetadata; JSON body properties carry them on JsonPropertyInfo's
        // reflection metadata instead. Check both since this transformer runs for every schema.
        AllowedValuesAttribute? allowedValuesAttribute =
            context.ParameterDescription?.ModelMetadata.ValidatorMetadata
                .OfType<AllowedValuesAttribute>()
                .FirstOrDefault()
            ?? context.JsonPropertyInfo?.AttributeProvider?
                .GetCustomAttributes(typeof(AllowedValuesAttribute), true)
                .OfType<AllowedValuesAttribute>()
                .FirstOrDefault();

        if (allowedValuesAttribute is not null)
        {
            schema.Enum = allowedValuesAttribute.Values
                .Select(value => (JsonNode)JsonValue.Create(value?.ToString())!)
                .ToList();
        }

        return Task.CompletedTask;
    }
}