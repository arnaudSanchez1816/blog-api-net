using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Asp.Versioning;
using BlogApi.Authorization;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Mapping;
using BlogApi.Routes.V1;
using BlogApi.Services.Tags;
using BlogApi.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers.V1;

/// <summary>
/// Tag-related endpoints
/// </summary>
[ApiVersion(1)]
[ApiController]
[Route(ApiRoutes.Tags.Base)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class TagsController : ControllerBase
{
    private readonly ITagsService _tagsService;

    public TagsController(ITagsService tagsService)
    {
        _tagsService = tagsService;
    }

    /// <summary>
    /// Get all tags
    /// </summary>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <response code="403"></response>
    /// <returns>A list of all tags in the application</returns>
    [HttpGet(ApiRoutes.Tags.GetAll)]
    [HasPermission(Permissions.Tags.Read)]
    public async Task<ActionResult<GetTagsResponse>> GetAllTags(CancellationToken ct)
    {
        List<Tag> tags = await _tagsService.GetAllTags(ct);

        GetTagsResponse response = new GetTagsResponse
        {
            Tags = tags.Select(t => t.ToTagResponse()).ToList(),
            Metadata = new PagedResponseMetadata
            {
                Count = tags.Count
            }
        };
        return Ok(response);
    }

    /// <summary>
    /// </summary>
    /// <param name="slug"></param>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <response code="400">Invalid slug</response>
    /// <response code="403"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpGet(ApiRoutes.Tags.GetBySlug)]
    [HasPermission(Permissions.Tags.Read)]
    public async Task<ActionResult<TagResponse>> GetBySlug(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug, CancellationToken ct)
    {
        Tag? tag = await _tagsService.GetTag(slug, ct);
        if (tag == null)
        {
            return NotFound();
        }

        return Ok(tag.ToTagResponse());
    }

    /// <summary>
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <response code="403"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpGet(ApiRoutes.Tags.GetById)]
    [HasPermission(Permissions.Tags.Read)]
    public async Task<ActionResult<TagResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        Tag? tag = await _tagsService.GetTag(id, ct);
        if (tag == null)
        {
            return NotFound();
        }

        return Ok(tag.ToTagResponse());
    }

    /// <summary>
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="401"></response>
    /// <response code="403"></response>
    /// <returns></returns>
    [HttpPost(ApiRoutes.Tags.Create)]
    [HasPermission(Permissions.Tags.Create)]
    public async Task<ActionResult<TagResponse>> Create([FromBody] CreateTagRequest request, CancellationToken ct)
    {
        Tag newTag = request.ToTag();
        newTag = await _tagsService.CreateTag(newTag, ct);

        return CreatedAtAction(nameof(GetBySlug), new { slug = newTag.Slug }, newTag.ToTagResponse());
    }

    /// <summary>
    /// </summary>
    /// <param name="slug"></param>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="401"></response>
    /// <response code="403"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpPut(ApiRoutes.Tags.UpdateBySlug)]
    [HasPermission(Permissions.Tags.Update)]
    public async Task<ActionResult<TagResponse>> UpdateBySlug(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug, [FromBody] UpdateTagRequest request, CancellationToken ct)
    {
        Tag? tag = await _tagsService.GetTag(slug, ct);
        if (tag is null)
        {
            return NotFound();
        }

        await _tagsService.UpdateTag(tag, request.Name, request.Slug, ct);

        return Ok(tag.ToTagResponse());
    }

    /// <summary>
    /// </summary>
    /// <param name="slug"></param>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="401"></response>
    /// <response code="403"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpDelete(ApiRoutes.Tags.DeleteBySlug)]
    [HasPermission(Permissions.Tags.Delete)]
    public async Task<ActionResult<TagResponse>> DeleteBySlug(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug, CancellationToken ct)
    {
        Tag? tag = await _tagsService.GetTag(slug, ct);
        if (tag is null)
        {
            return NotFound();
        }

        await _tagsService.DeleteTag(tag, ct);
        return Ok(tag.ToTagResponse());
    }
}