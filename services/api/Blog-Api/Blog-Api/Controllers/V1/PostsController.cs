using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Asp.Versioning;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Routes.V1;
using BlogApi.Services;
using BlogApi.Utils;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers.V1;

[ApiVersion(1)]
[ApiController]
[Route(ApiRoutes.Posts.Base)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class PostsController : ControllerBase
{
    private readonly IPostsService _postsService;

    public PostsController(IPostsService postsService)
    {
        _postsService = postsService;
    }

    [HttpGet(ApiRoutes.Posts.GetBySlug, Name = "GetPostBySlug")]
    public async Task<ActionResult<PostResponse>> GetBySlug([FromRoute] [RegularExpression(SlugGenerator.Pattern)] string slug)
    {
        Post? post = await _postsService.GetPostBySlug(slug);

        if (post is null)
        {
            return NotFound();
        }

        return Ok(new PostResponse(post.Id, post.Title, post.Slug, post.Body));
    }
}