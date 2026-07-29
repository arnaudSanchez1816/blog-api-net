using System.Reflection;
using System.Text.Json.Serialization;
using BlogApi.Contracts.V1.Requests;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlogApi.ModelBinding;

public class PostSortOptionModelBinder : IModelBinder
{
    private static readonly Dictionary<string, PostSortOption> ValuesByToken = BuildTokenMap();

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        ValueProviderResult valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        string? rawValue = valueProviderResult.FirstValue;
        if (string.IsNullOrEmpty(rawValue))
        {
            return Task.CompletedTask;
        }

        if (ValuesByToken.TryGetValue(rawValue, out PostSortOption sortOption))
        {
            bindingContext.Result = ModelBindingResult.Success(sortOption);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
                $"The value '{rawValue}' is not valid for {bindingContext.ModelName}.");
        }

        return Task.CompletedTask;
    }

    // Builds the wire-token -> enum mapping straight from [JsonStringEnumMemberName], so the enum
    // stays the single source of truth for valid tokens.
    private static Dictionary<string, PostSortOption> BuildTokenMap()
    {
        Dictionary<string, PostSortOption> map = new Dictionary<string, PostSortOption>(StringComparer.Ordinal);

        foreach (PostSortOption value in Enum.GetValues<PostSortOption>())
        {
            FieldInfo field = typeof(PostSortOption).GetField(value.ToString())!;
            string token = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? value.ToString();
            map[token] = value;
        }

        return map;
    }
}

public class PostSortOptionModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Metadata.ModelType == typeof(PostSortOption) ? new PostSortOptionModelBinder() : null;
    }
}