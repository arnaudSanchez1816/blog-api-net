using BlogApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace BlogApi.Integration;

public class BlogApiFactory : WebApplicationFactory<IApiMarker>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:18").WithUsername("blogApiTest")
        .WithPassword("blogApiTest").WithDatabase("blogApiTest").Build();

    public HttpClient HttpClient { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        HttpClient = CreateClient();

        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        DataContext context = scope.ServiceProvider.GetRequiredService<DataContext>();
        await context.Database.MigrateAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<DataContext>>();
            services.AddDbContext<DataContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });
        });
    }
}