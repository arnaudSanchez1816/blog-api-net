using BlogApi.Validation.Resolvers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

namespace BlogApi.Installers;

public static class ValidationInstaller
{
    public static IServiceCollection InstallFluentValidation(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            // options.SuppressModelStateInvalidFilter = true;
        });

        ValidatorOptions.Global.DisplayNameResolver = CamelCasePropertyNameResolver.ResolvePropertyName;

        services.AddValidatorsFromAssemblyContaining<IApiMarker>();

        services.AddFluentValidationAutoValidation(configuration =>
        {
            // configuration.DisableBuiltInModelValidation = true;
        });

        return services;
    }
}