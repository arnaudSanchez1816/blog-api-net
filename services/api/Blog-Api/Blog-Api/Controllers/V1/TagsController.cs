using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Asp.Versioning;
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
    /// <returns>A list of all tags in the application</returns>
    [HttpGet(ApiRoutes.Tags.GetAll)]
    public async Task<ActionResult<GetTagsResponse>> GetAllTags()
    {
        List<Tag> tags = await _tagsService.GetAllTags();

        GetTagsResponse response = new GetTagsResponse
        {
            Tags = tags.Select(t => t.ToTagResponse()).ToList(),
            Metadata = new PaginationQueryMetadata
            {
                Count = tags.Count
            }
        };
        return Ok(response);
    }

    [HttpGet(ApiRoutes.Tags.GetBySlug)]
    public async Task<ActionResult<TagResponse>> GetBySlug(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug)
    {
        Tag? tag = await _tagsService.GetTag(slug);
        if (tag == null)
        {
            return NotFound();
        }

        return Ok(tag.ToTagResponse());
    }

    [HttpGet(ApiRoutes.Tags.GetById)]
    public async Task<ActionResult<TagResponse>> GetById([FromRoute] Guid id)
    {
        Tag? tag = await _tagsService.GetTag(id);
        if (tag == null)
        {
            return NotFound();
        }

        return Ok(tag.ToTagResponse());
    }

    [HttpPost(ApiRoutes.Tags.Create)]
    public async Task<ActionResult<TagResponse>> Create([FromBody] CreateTagRequest request)
    {
        Tag newTag = request.ToTag();
        newTag = await _tagsService.CreateTag(newTag);

        return CreatedAtAction(nameof(GetBySlug), new { slug = newTag.Slug }, newTag.ToTagResponse());
    }

    [HttpPut(ApiRoutes.Tags.UpdateBySlug)]
    public async Task<ActionResult<TagResponse>> UpdateBySlug(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug, [FromBody] UpdateTagRequest request)
    {
        Tag? tag = await _tagsService.GetTag(slug);
        if (tag is null)
        {
            return NotFound();
        }

        tag.Name = request.Name ?? tag.Name;
        tag.Slug = request.Slug ?? tag.Slug;
        await _tagsService.UpdateTag(tag);

        return Ok(tag.ToTagResponse());
    }

    [HttpDelete(ApiRoutes.Tags.DeleteBySlug)]
    public async Task<ActionResult<TagResponse>> DeleteBySlug(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug)
    {
        Tag? tag = await _tagsService.GetTag(slug);
        if (tag is null)
        {
            return NotFound();
        }

        await _tagsService.DeleteTag(tag);
        return Ok(tag.ToTagResponse());
    }
}