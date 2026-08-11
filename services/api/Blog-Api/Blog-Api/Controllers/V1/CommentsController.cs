using System.Net.Mime;
using Asp.Versioning;
using BlogApi.Authorization;
using BlogApi.Contracts.V1.Requests;
using BlogApi.Contracts.V1.Responses;
using BlogApi.Domain;
using BlogApi.Mapping;
using BlogApi.Routes.V1;
using BlogApi.Services.Comments;
using Microsoft.AspNetCore.Mvc;

namespace BlogApi.Controllers.V1;

[ApiVersion(1)]
[ApiController]
[Route(ApiRoutes.Comments.Base)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class CommentsController : ControllerBase
{
    private readonly ICommentsService _commentsService;

    public CommentsController(ICommentsService commentsService)
    {
        _commentsService = commentsService;
    }

    /// <summary>
    /// Get a comment with the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="403"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpGet(ApiRoutes.Comments.GetById)]
    [HasPermission(Permissions.Comments.Read)]
    public async Task<ActionResult<CommentResponse>> GetById([FromRoute] Guid id)
    {
        Comment? comment = await _commentsService.GetCommentById(id);
        if (comment is null)
        {
            return NotFound();
        }

        return Ok(comment.ToCommentResponse());
    }

    /// <summary>
    /// Update a comment with the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="401"></response>
    /// <response code="403"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpPut(ApiRoutes.Comments.UpdateById)]
    [HasPermission(Permissions.Comments.Update)]
    public async Task<ActionResult<CommentResponse>> UpdateById([FromRoute] Guid id,
        [FromBody] UpdateCommentRequest request)
    {
        Comment? comment = await _commentsService.GetCommentById(id);
        if (comment is null)
        {
            return NotFound();
        }

        await _commentsService.UpdateComment(comment, request.Username, request.Body);

        return Ok(comment.ToCommentResponse());
    }

    /// <summary>
    /// Delete a comment with the given id.
    /// </summary>
    /// <param name="id"></param>
    /// <response code="200"></response>
    /// <response code="400"></response>
    /// <response code="401"></response>
    /// <response code="403"></response>
    /// <response code="404"></response>
    /// <returns></returns>
    [HttpDelete(ApiRoutes.Comments.DeleteById)]
    [HasPermission(Permissions.Comments.Delete)]
    public async Task<ActionResult<CommentResponse>> DeleteById([FromRoute] Guid id)
    {
        Comment? comment = await _commentsService.GetCommentById(id);
        if (comment is null)
        {
            return NotFound();
        }

        await _commentsService.DeleteComment(comment);
        return Ok(comment);
    }
}