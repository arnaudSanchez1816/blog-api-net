using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Asp.Versioning;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Mapping;
using BlogApi.Routes.V1;
using BlogApi.Services.Posts;
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

    /// <summary>
    /// Get a post by its slug.
    /// </summary>
    /// <param name="slug"></param>
    /// <response code="200"></response>
    /// <response code="400">When slug is bad</response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpGet(ApiRoutes.Posts.GetBySlug, Name = "GetPostBySlug")]
    public async Task<ActionResult<PostResponse>> GetBySlug(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug)
    {
        Post? post = await _postsService.GetPostBySlugWithTags(slug);

        if (post is null)
        {
            return NotFound();
        }

        return Ok(post.ToPostResponse());
    }

    /// <summary>
    /// Create a new post with the given title.
    /// </summary>
    /// <param name="request"></param>
    /// <response code="201"></response>
    /// <response code="400"></response>
    /// <returns></returns>
    [HttpPost(ApiRoutes.Posts.Create)]
    public async Task<ActionResult<PostResponse>> CreatePost([FromBody] CreatePostRequest request)
    {
        string postSlug = await _postsService.GenerateUniqueSlugAsync(request.Title);

        Post newPost = new Post
        {
            Title = request.Title,
            Slug = postSlug,
            AuthorId = Guid.NewGuid() // TODO : replace with authenticated user id
        };
        newPost = await _postsService.CreatePost(newPost);

        return CreatedAtAction(nameof(GetBySlug), new { slug = newPost.Slug }, newPost.ToPostResponse());
    }

    /// <summary>
    /// Update a post.
    /// </summary>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpPut(ApiRoutes.Posts.UpdateBySlug)]
    public async Task<ActionResult<PostResponse>> UpdatePost(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug,
        [FromBody] UpdatePostRequest request)
    {
        return Ok();
    }

    /// <summary>
    /// Delete a post.
    /// </summary>
    /// <response code="200"></response>
    /// <response code="400">When slug is bad</response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpDelete(ApiRoutes.Posts.DeleteBySlug)]
    public async Task<ActionResult<PostResponse>> DeletePost(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug)
    {
        Post? post = await _postsService.GetPostBySlugWithTags(slug);
        if (post is null)
        {
            return NotFound();
        }

        await _postsService.DeletePost(post);

        return Ok(post.ToPostResponse());
    }
}