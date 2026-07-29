using BlogApi.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Installers;

public static class ModelBindingInstaller
{
    public static IServiceCollection InstallCustomModelBinders(this IServiceCollection services)
    {
        services.Configure<MvcOptions>(options =>
        {
            options.ModelBinderProviders.Insert(0, new PostSortOptionModelBinderProvider());
        });

        return services;
    }
}
