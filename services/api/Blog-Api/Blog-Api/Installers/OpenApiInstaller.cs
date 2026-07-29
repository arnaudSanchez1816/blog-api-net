using System.Text.Json;
using BlogApi.Options;
using BlogApi.Transformers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace BlogApi.Installers;

public static class OpenApiInstaller
{
    private const string BearerJwtScheme = "Bearer";

    public static IServiceCollection InstallOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddSchemaTransformer<AllowedValuesSchemaTransformer>();

            // Add Bearer security scheme 
            options.AddDocumentTransformer((document, context, _) =>
            {
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    [BearerJwtScheme] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = BearerJwtScheme,
                        BearerFormat = "JWT",
                        Description = "JWT authorization header using the Bearer scheme."
                    }
                };

                return Task.CompletedTask;
            });

            // Mark endpoints with authorize attribute as secured with Bearer scheme
            options.AddOperationTransformer((operation, context, _) =>
            {
                bool isAuthorizeEndpoint = context.Description.ActionDescriptor.EndpointMetadata
                    .OfType<AuthorizeAttribute>().Any();
                if (isAuthorizeEndpoint)
                {
                    operation.Security =
                    [
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference(BearerJwtScheme, context.Document)] = []
                        }
                    ];
                }

                return Task.CompletedTask;
            });

            // Convert Requests/Responses parameters to camel case
            options.AddOperationTransformer((operation, context, _) =>
            {
                foreach (IOpenApiParameter parameter in operation.Parameters ?? [])
                {
                    if (parameter is OpenApiParameter { Name: not null } openApiParameter)
                    {
                        openApiParameter.Name = JsonNamingPolicy.CamelCase.ConvertName(openApiParameter.Name);
                    }
                }

                return Task.CompletedTask;
            });

            // GetPostsRequest.IncludeUnpublished is only ever bound via the "unpublished" query
            // parameter declared explicitly on the action; the "includeUnpublished" entry below is a
            // dead duplicate produced by complex-type FromQuery expansion.
            options.AddOperationTransformer((operation, context, _) =>
            {
                IOpenApiParameter? deadParameter = operation.Parameters?.FirstOrDefault(parameter =>
                    string.Equals(parameter.Name, "includeUnpublished", StringComparison.Ordinal));
                if (deadParameter is not null)
                {
                    operation.Parameters!.Remove(deadParameter);
                }

                return Task.CompletedTask;
            });
        });

        // Bind open api options
        OpenApiOptions openApiOptions = new OpenApiOptions();
        configuration.Bind(nameof(OpenApiOptions), openApiOptions);
        services.AddSingleton(openApiOptions);

        return services;
    }

    public static WebApplication InstallScalar(this WebApplication app, string? defaultBearerToken = null)
    {
        OpenApiOptions options = app.Services.GetRequiredService<OpenApiOptions>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi(options.JsonRoute);
        }

        app.MapScalarApiReference(options.UiEndpoint, x =>
        {
            x.Title = options.Title;
            x.OpenApiRoutePattern = options.JsonRoute;
            x.WithClassicLayout();
            x.AddPreferredSecuritySchemes(BearerJwtScheme);
            if (defaultBearerToken != null)
            {
                x.AddHttpAuthentication(BearerJwtScheme, auth => auth.Token = defaultBearerToken);
            }
        });

        return app;
    }
}