using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Asp.Versioning;
using BlogApi.Authorization;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Requests.Queries;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Extensions;
using BlogApi.Mapping;
using BlogApi.Repositories.Posts;
using BlogApi.Routes.V1;
using BlogApi.Services.Posts;
using BlogApi.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers.V1;

[ApiVersion(1)]
[ApiController]
[Route(ApiRoutes.Posts.Base)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class PostsController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAuthorizationService _authorizationService;
    private readonly IPostsService _postsService;

    public PostsController(IPostsService postsService, IAuthorizationService authorizationService,
        IAuthenticationService authenticationService)
    {
        _postsService = postsService;
        _authorizationService = authorizationService;
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Get a paginated list of posts.
    /// </summary>
    /// <param name="filterQuery"></param>
    /// <param name="paginationQuery"></param>
    /// <param name="includeUnpublished"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="403"></response>
    /// <returns></returns>
    [HttpGet(ApiRoutes.Posts.GetAll)]
    [HasPermission(Permissions.Posts.Read)]
    public async Task<ActionResult<GetPostsResponse>> GetPosts(
        [FromQuery] GetPostsFilterQuery filterQuery,
        [FromQuery] PaginationQuery paginationQuery,
        [FromQuery(Name = "unpublished")] bool includeUnpublished)
    {
        // Check that a user is authenticated and if it can read unpublished posts.
        AuthenticateResult authenticateResult =
            await _authenticationService.AuthenticateAsync(HttpContext, JwtBearerDefaults.AuthenticationScheme);
        bool canReadDraftPosts = false;
        if (authenticateResult.Succeeded)
        {
            AuthorizationResult authorizationResult = await _authorizationService.AuthorizeAsync(
                authenticateResult.Principal!,
                Permissions.ToPermissionPolicy(Permissions.Posts.ReadUnpublished));
            canReadDraftPosts = authorizationResult.Succeeded;
        }

        // Only allows viewing unpublished posts if authenticated and can read unpublished posts
        filterQuery = filterQuery with
        {
            IncludeUnpublished = canReadDraftPosts && includeUnpublished
        };

        PagedPostsResult result = await _postsService.GetPosts(filterQuery, paginationQuery);
        List<PostResponse> mappedPosts = result.Posts.Select(p => p.ToPostResponse()).ToList();

        return Ok(GetPostsResponse.Create(mappedPosts, result.TotalCount, filterQuery, paginationQuery));
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

        if (!post.IsPublished)
        {
            AuthenticateResult authenticateResult =
                await _authenticationService.AuthenticateAsync(HttpContext, JwtBearerDefaults.AuthenticationScheme);
            if (!authenticateResult.Succeeded)
            {
                return NotFound();
            }

            AuthorizationResult authorizationCheck = await _authorizationService.AuthorizeAsync(
                authenticateResult.Principal,
                post,
                Permissions.ToPermissionPolicy(Permissions.Posts.ReadUnpublished));
            if (!authorizationCheck.Succeeded)
            {
                // Maybe 403 ?
                return NotFound();
            }
        }


        return Ok(post.ToPostResponse());
    }

    /// <summary>
    /// Create a new post with the given title.
    /// </summary>
    /// <param name="request"></param>
    /// <response code="201"></response>
    /// <response code="400"></response>
    /// <response code="401"></response>
    /// <response code="403"></response>
    /// <returns></returns>
    [HttpPost(ApiRoutes.Posts.Create)]
    [HasPermission(Permissions.Posts.Create)]
    public async Task<ActionResult<PostResponse>> CreatePost([FromBody] CreatePostRequest request)
    {
        string postSlug = await _postsService.GenerateUniqueSlugAsync(request.Title);

        Post newPost = new Post
        {
            Title = request.Title,
            Slug = postSlug,
            AuthorId = User.GetUserId()
        };
        newPost = await _postsService.CreatePost(newPost);

        return CreatedAtAction(nameof(GetBySlug), new { slug = newPost.Slug }, newPost.ToPostResponse());
    }

    /// <summary>
    /// Update a post.
    /// </summary>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="401"></response>
    /// <response code="403"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpPut(ApiRoutes.Posts.UpdateBySlug)]
    [Authorize]
    public async Task<ActionResult<PostResponse>> UpdatePost(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug,
        [FromBody] UpdatePostRequest request)
    {
        Post? post = await _postsService.GetPostBySlugWithTags(slug);
        if (post is null)
        {
            return NotFound();
        }

        AuthorizationResult permissionCheck = await _authorizationService.AuthorizeAsync(User,
            post,
            Permissions.ToPermissionPolicy(Permissions.Posts.Update));
        if (!permissionCheck.Succeeded)
        {
            return Forbid();
        }

        await _postsService.UpdatePost(post, request);

        return Ok(post.ToPostResponse());
    }

    /// <summary>
    /// Delete a post.
    /// </summary>
    /// <response code="200"></response>
    /// <response code="400">When slug is bad</response>
    /// <response code="401"></response>
    /// <response code="403"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpDelete(ApiRoutes.Posts.DeleteBySlug)]
    [Authorize]
    public async Task<ActionResult<PostResponse>> DeletePost(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug)
    {
        Post? post = await _postsService.GetPostBySlugWithTags(slug);
        if (post is null)
        {
            return NotFound();
        }

        AuthorizationResult authorizationResult = await _authorizationService.AuthorizeAsync(User,
            post,
            Permissions.ToPermissionPolicy(Permissions.Posts.Delete));
        if (!authorizationResult.Succeeded)
        {
            return Forbid();
        }

        await _postsService.DeletePost(post);

        return Ok(post.ToPostResponse());
    }

    /// <summary>
    /// Get the comments of a given post.
    /// </summary>
    /// <param name="slug"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpGet(ApiRoutes.Posts.GetCommentsBySlug)]
    public async Task<ActionResult<GetPostCommentsResponse>> GetPostCommentsBySlug(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug)
    {
        Post? post = await _postsService.GetPostBySlugWithComments(slug);
        if (post is null)
        {
            return NotFound();
        }

        if (!post.IsPublished)
        {
            AuthenticateResult authResult =
                await _authenticationService.AuthenticateAsync(HttpContext, JwtBearerDefaults.AuthenticationScheme);
            if (!authResult.Succeeded)
            {
                return NotFound();
            }

            AuthorizationResult permissionCheck = await _authorizationService.AuthorizeAsync(authResult.Principal,
                post,
                [
                    new PermissionRequirement(Permissions.Posts.Read),
                    new PermissionRequirement(Permissions.Comments.Read)
                ]);
            if (!permissionCheck.Succeeded)
            {
                return NotFound();
            }
        }

        return Ok(post.ToGetPostCommentsResponse());
    }

    /// <summary>
    /// Create a comment for the given post.
    /// </summary>
    /// <param name="slug"></param>
    /// <param name="request"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="403"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpPost(ApiRoutes.Posts.CreateCommentBySlug)]
    [HasPermission(Permissions.Comments.Create)]
    public async Task<ActionResult<CommentResponse>> CreatePostComment(
        [FromRoute] [RegularExpression(SlugGenerator.Pattern)]
        string slug, [FromBody] CreatePostCommentRequest request)
    {
        Post? post = await _postsService.GetPostBySlug(slug);
        if (post is null || !post.IsPublished)
        {
            return NotFound();
        }

        Comment comment = await _postsService.CreateCommentForPost(post, request.Username, request.Body);

        return Ok(comment.ToCommentResponse());
    }
}