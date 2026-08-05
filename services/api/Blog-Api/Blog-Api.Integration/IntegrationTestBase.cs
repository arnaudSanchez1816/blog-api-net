using Microsoft.Extensions.DependencyInjection;

namespace BlogApi.Integration;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private AsyncServiceScope _scope;

    protected BlogApiFactory Factory { get; }

    protected HttpClient HttpClient
    {
        get => Factory.HttpClient;
    }

    public IntegrationTestBase(BlogApiFactory factory)
    {
        Factory = factory;
    }

    public async ValueTask DisposeAsync()
    {
        await OnDisposeAsync();
        await _scope.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
        _scope = Factory.Services.CreateAsyncScope();
        await OnInitializeAsync();
    }

    protected virtual Task OnInitializeAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected T GetRequiredService<T>() where T : notnull
    {
        return _scope.ServiceProvider.GetRequiredService<T>();
    }
}