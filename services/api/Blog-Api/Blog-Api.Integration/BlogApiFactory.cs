using System.Text.Json;
using BlogApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace BlogApi.Integration;

public class BlogApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18").WithUsername("blogApiTest")
        .WithPassword("blogApiTest")
        .WithDatabase("blogApiTest")
        .Build();

    private NpgsqlConnection _dbConnection = null!;

    private Respawner _respawner = null!;

    public HttpClient HttpClient { get; private set; } = null!;

    public FakeTimeProvider TimeProvider { get; } = new FakeTimeProvider(DateTimeOffset.UtcNow);

    // Some Installers (e.g. AuthenticationInstaller) read IConfiguration eagerly in Program.cs, before
    // builder.Build() runs. WebApplicationFactory's ConfigureAppConfiguration/ConfigureTestServices hooks
    // only apply once Build() is reached, so they're too late for those reads. Environment variables are
    // read synchronously as soon as WebApplication.CreateBuilder() runs, so seed them from
    // appsettings.Testing.json before any host gets created.
    static BlogApiFactory()
    {
        string appsettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json");
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(appsettingsPath));
        SetEnvironmentVariablesFromJson(document.RootElement, null);
    }

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        HttpClient = CreateClient();

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        DataContext context = scope.ServiceProvider.GetRequiredService<DataContext>();
        await context.Database.MigrateAsync();

        _dbConnection = new NpgsqlConnection(_dbContainer.GetConnectionString());
        await _dbConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_dbConnection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = ["__EFMigrationsHistory"]
            });
    }

    public new async ValueTask DisposeAsync()
    {
        await _dbConnection.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    private static void SetEnvironmentVariablesFromJson(JsonElement element, string? prefix)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string key = prefix is null ? property.Name : $"{prefix}__{property.Name}";
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                SetEnvironmentVariablesFromJson(property.Value, key);
            }
            else
            {
                Environment.SetEnvironmentVariable(key, property.Value.ToString());
            }
        }
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<DataContext>>();
            services.AddDbContext<DataContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TimeProvider);
        });
    }
}