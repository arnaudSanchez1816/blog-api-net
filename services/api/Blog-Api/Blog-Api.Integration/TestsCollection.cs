namespace BlogApi.Integration;

[CollectionDefinition(nameof(TestsCollection))]
public class TestsCollection : ICollectionFixture<BlogApiFactory>
{
}