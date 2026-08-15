using System.Net.Mime;
using Asp.Versioning;
using BlogApi.Contracts.V1.Requests.Queries;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Extensions;
using BlogApi.Mapping;
using BlogApi.Repositories.Posts;
using BlogApi.Routes.V1;
using BlogApi.Services.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers.V1;

[ApiVersion(1)]
[ApiController]
[Route(ApiRoutes.Users.Base)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class UsersController : ControllerBase
{
    private readonly IPostsService _postsService;
    private readonly UserManager<BlogUser> _userManager;

    public UsersController(IPostsService postsService, UserManager<BlogUser> userManager)
    {
        _postsService = postsService;
        _userManager = userManager;
    }

    /// <summary>
    /// Returns the user details of the authenticated user.
    /// </summary>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <response code="401"></response>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [HttpGet(ApiRoutes.Users.GetCurrentUser)]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetCurrentUser(CancellationToken ct)
    {
        BlogUser? user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            throw new InvalidOperationException("Authenticated user is null");
        }

        return Ok(user.ToUserResponse());
    }

    /// <summary>
    /// Returns posts created by the user.
    /// </summary>
    /// <param name="filterQuery"></param>
    /// <param name="paginationQuery"></param>
    /// <param name="ct"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="401"></response>
    /// <returns></returns>
    [HttpGet(ApiRoutes.Users.GetCurrentUserPosts)]
    [Authorize]
    public async Task<ActionResult<GetPostsResponse>> GetCurrentUserPosts(
        [FromQuery] GetPostsFilterQuery filterQuery,
        [FromQuery] PaginationQuery paginationQuery, CancellationToken ct)
    {
        Guid userId = User.GetUserId();
        // Always include unpublished when getting user posts
        filterQuery = filterQuery with { IncludeUnpublished = true, Author = userId };
        PagedPostsResult pagedPostsResult = await _postsService.GetPosts(filterQuery, paginationQuery, ct);

        List<PostResponse> mappedPosts = pagedPostsResult.Posts.Select(p => p.ToPostResponse()).ToList();
        return Ok(GetPostsResponse.Create(mappedPosts, pagedPostsResult.TotalCount, filterQuery, paginationQuery));
    }
}