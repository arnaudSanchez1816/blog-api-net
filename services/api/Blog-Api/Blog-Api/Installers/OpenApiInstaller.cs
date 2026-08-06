using System.Text.Json;
using BlogApi.Authentication;
using BlogApi.Controllers.V1;
using BlogApi.Options;
using BlogApi.Transformers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace BlogApi.Installers;

public static class OpenApiInstaller
{
    public static IServiceCollection InstallOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi("v1",
            options =>
            {
                options.AddSchemaTransformer<AllowedValuesSchemaTransformer>();

                // Add security schemes 
                options.AddDocumentTransformer((document, context, _) =>
                {
                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                    {
                        [JwtBearerDefaults.AuthenticationScheme] = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = JwtBearerDefaults.AuthenticationScheme,
                            BearerFormat = "JWT",
                            Description = "JWT authorization header using the Bearer scheme."
                        },
                        [RefreshTokenAuthDefaults.RefreshTokenScheme] = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.ApiKey,
                            In = ParameterLocation.Cookie,
                            Name = RefreshTokenAuthDefaults.RefreshTokenCookie,
                            Scheme = RefreshTokenAuthDefaults.RefreshTokenScheme,
                            Description = "Refresh token in a HttpOnly cookie."
                        }
                    };

                    return Task.CompletedTask;
                });

                // Mark endpoints with authorize attribute as secured
                options.AddOperationTransformer((operation, context, _) =>
                {
                    List<AuthorizeAttribute> authorizeAttributes = context.Description.ActionDescriptor
                        .EndpointMetadata
                        .OfType<AuthorizeAttribute>()
                        .ToList();
                    if (authorizeAttributes.Count > 0)
                    {
                        operation.Security = [];

                        if (authorizeAttributes.Any(att =>
                                string.IsNullOrEmpty(att.AuthenticationSchemes) ||
                                att.AuthenticationSchemes.Contains(JwtBearerDefaults.AuthenticationScheme)))
                        {
                            // Jwt bearer auth
                            operation.Security.Add(new OpenApiSecurityRequirement
                            {
                                [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme,
                                        context.Document)] =
                                    []
                            });
                        }

                        if (authorizeAttributes.Any(att =>
                                !string.IsNullOrEmpty(att.AuthenticationSchemes) &&
                                att.AuthenticationSchemes.Contains(RefreshTokenAuthDefaults.RefreshTokenScheme)))
                        {
                            // Refresh token auth
                            operation.Security.Add(new OpenApiSecurityRequirement
                            {
                                [new OpenApiSecuritySchemeReference(RefreshTokenAuthDefaults.RefreshTokenScheme,
                                        context.Document)] =
                                    []
                            });
                        }
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

                // Remove includeUnpublished and author parameters for the UsersController.GetCurrentUserPosts since they are always overriden
                options.AddOperationTransformer((operation, context, _) =>
                {
                    if (context.Description.ActionDescriptor.RouteValues["action"] !=
                        nameof(UsersController.GetCurrentUserPosts))
                    {
                        return Task.CompletedTask;
                    }

                    List<IOpenApiParameter>? deadParameters = operation.Parameters?.Where(parameter =>
                            string.Equals(parameter.Name, "includeUnpublished", StringComparison.Ordinal) ||
                            string.Equals(parameter.Name, "author", StringComparison.Ordinal))
                        .ToList();
                    if (deadParameters is not null)
                    {
                        foreach (IOpenApiParameter parameter in deadParameters)
                        {
                            operation.Parameters!.Remove(parameter);
                        }
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

        app.MapScalarApiReference(options.UiEndpoint,
            x =>
            {
                x.Title = options.Title;
                x.OpenApiRoutePattern = options.JsonRoute;
                x.WithClassicLayout();
                x.AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme);
                if (defaultBearerToken != null)
                {
                    x.AddHttpAuthentication(JwtBearerDefaults.AuthenticationScheme,
                        auth => auth.Token = defaultBearerToken);
                }
            });

        return app;
    }
}