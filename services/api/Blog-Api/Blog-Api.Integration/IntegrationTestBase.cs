namespace BlogApi.Integration;

public class IntegrationTestBase : IAsyncLifetime
{
    protected BlogApiFactory Factory { get; }

    public IntegrationTestBase(BlogApiFactory factory)
    {
        Factory = factory;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public async ValueTask InitializeAsync()
    {
        await Factory.ResetDatabaseAsync();
    }
}