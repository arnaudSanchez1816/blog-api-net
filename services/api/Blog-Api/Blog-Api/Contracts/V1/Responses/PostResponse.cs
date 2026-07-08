namespace BlogApi.Contracts.V1.Responses;

public record PostResponse(Guid Id, string Title, string Slug, string Body);