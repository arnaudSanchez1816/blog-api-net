using BlogApi.Contracts.V1.Responses;
using BlogApi.Data;
using BlogApi.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogApi.Controllers.V1;

[ApiController]
[Route("posts")]
public class PostsController : ControllerBase
{
    private readonly DataContext _context;

    public PostsController(DataContext context)
    {
        _context = context;
    }

    [HttpGet("{slug}", Name = "GetPostBySlug")]
    public async Task<ActionResult<PostResponse>> GetBySlug(string slug)
    {
        Post? post = await _context.Posts
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Slug == slug);

        if (post is null)
        {
            return NotFound();
        }

        return Ok(new PostResponse(post.Id, post.Title, post.Slug, post.Body));
    }
}