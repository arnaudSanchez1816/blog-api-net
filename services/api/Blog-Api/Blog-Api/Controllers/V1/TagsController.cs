using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Asp.Versioning;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Routes.V1;
using BlogApi.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers.V1;

[ApiVersion(1)]
[ApiController]
[Route(ApiRoutes.Tags.Base)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class TagsController : ControllerBase
{
    [HttpGet(ApiRoutes.Tags.GetAll)]
    public async Task<IActionResult> GetAllTags()
    {
        return Ok();
    }

    [HttpGet(ApiRoutes.Tags.GetBySlug)]
    public async Task<ActionResult<TagResponse>> GetBySlug([FromRoute][RegularExpression(SlugGenerator.Pattern)] string slug)
    {
        return Ok();
    }

    [HttpGet(ApiRoutes.Tags.GetById)]
    public async Task<ActionResult<TagResponse>> GetById([FromRoute] Guid id)
    {
        return Ok();
    }

    [HttpPost(ApiRoutes.Tags.Create)]
    public async Task<ActionResult<TagResponse>> Create([FromBody] CreateTagRequest request)
    {
        return Ok();
    }

    [HttpPut(ApiRoutes.Tags.UpdateBySlug)]
    public async Task<ActionResult<TagResponse>> UpdateBySlug([FromRoute][RegularExpression(SlugGenerator.Pattern)] string slug)
    {
        return Ok();
    }

    [HttpDelete(ApiRoutes.Tags.DeleteBySlug)]
    public async Task<ActionResult<TagResponse>> DeleteBySlug([FromRoute][RegularExpression(SlugGenerator.Pattern)] string slug)
    {
        return Ok();
    }
}
