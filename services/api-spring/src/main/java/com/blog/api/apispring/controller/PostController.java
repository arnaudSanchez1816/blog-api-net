package com.blog.api.apispring.controller;

import com.blog.api.apispring.config.SwaggerConfig;
import com.blog.api.apispring.dto.metadata.Metadata;
import com.blog.api.apispring.dto.posts.*;
import com.blog.api.apispring.dto.tag.TagIdOrSlug;
import com.blog.api.apispring.exception.PostPublicationConflictException;
import com.blog.api.apispring.model.Comment;
import com.blog.api.apispring.model.Post;
import com.blog.api.apispring.projection.CommentInfo;
import com.blog.api.apispring.projection.PostInfoWithAuthor;
import com.blog.api.apispring.projection.PostInfoWithAuthorAndTags;
import com.blog.api.apispring.security.userdetails.BlogUserDetails;
import com.blog.api.apispring.service.CommentService;
import com.blog.api.apispring.service.PostService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import org.jspecify.annotations.NonNull;
import org.springframework.data.domain.Page;
import org.springframework.http.HttpStatus;
import org.springframework.http.ProblemDetail;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.util.UriComponentsBuilder;

import java.net.URI;
import java.time.OffsetDateTime;
import java.util.*;

@Tag(name = "posts", description = "Posts related endpoints")
@RestController
@RequestMapping("/posts")
class PostController
{
	private final PostService postService;
	private final CommentService commentService;

	public PostController(PostService postService, CommentService commentService)
	{
		this.postService = postService;
		this.commentService = commentService;
	}

	@Operation(summary = "Get a list of posts", tags = {"posts"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = GetPostsResponse.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content)
	})
	@GetMapping
	public ResponseEntity<GetPostsResponse> getPosts(@Valid GetPostsRequestImpl getPostsRequest,
													 Authentication authentication)
	{
		if (authentication == null || !authentication.isAuthenticated())
		{
			// Only allow viewing unpublished
			getPostsRequest.setUnpublished(false);
		}

		Page<PostInfoWithAuthorAndTags> postsPage = postService.getPageablePostsInfo(getPostsRequest);
		List<PostInfoWithAuthorAndTags> postsContent = postsPage.getContent();
		Map<Long, Long> commentsCount = postService.getCommentsCount(postsContent.stream()
																				 .map(PostInfoWithAuthor::getId)
																				 .toList());
		List<PostDto> results = postsContent.stream()
											.map(p ->
											{
												PostDto dto = new PostDto(p);
												dto.setCommentsCount(commentsCount.get(p.getId()));
												return dto;
											})
											.toList();

		Metadata metadata = new Metadata();
		metadata.count(postsPage.getTotalElements())
				.page(postsPage.getNumber())
				.pageSize(postsPage.getSize())
				.sortBy(getPostsRequest.getSortBy());
		return ResponseEntity.ok(new GetPostsResponse(results, metadata));
	}

	@Operation(summary = "Get a post details", tags = {"posts"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = PostDto.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Post not found", content = @Content)
	})
	@GetMapping("/{id}")
	public ResponseEntity<PostDto> getPost(@PathVariable long id)
	{
		Optional<PostInfoWithAuthorAndTags> optionalPost = postService.getPostInfoWithTags(id);
		if (optionalPost.isEmpty())
		{
			return ResponseEntity.notFound()
								 .build();
		}
		PostInfoWithAuthorAndTags post = optionalPost.get();
		Long commentsCount = postService.getCommentsCount(post.getId());
		PostDto dto = new PostDto(post);
		dto.setCommentsCount(commentsCount);
		return ResponseEntity.ok(dto);
	}

	@Operation(summary = "Get a post details", tags = {"posts"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = PostDto.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Post not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
						 content = @Content)
	})
	@PostMapping
	@PreAuthorize("hasAuthority('CREATE')")
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	public ResponseEntity<PostDto> createPost(@Valid @RequestBody CreatePostRequest createPostRequest,
											  @AuthenticationPrincipal BlogUserDetails userDetails)
	{
		Post newPost = postService.createPost(createPostRequest.title(), userDetails.getId());

		URI location = UriComponentsBuilder.newInstance()
										   .path("/posts/{id}")
										   .buildAndExpand(newPost.getId())
										   .toUri();
		return ResponseEntity.created(location)
							 .body(new PostDto(newPost));
	}

	@Operation(summary = "Edit a post", tags = {"posts"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json", schema = @Schema(
												implementation = PostInfoWithAuthorAndTags.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Post not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
						 content = @Content)
	})
	@PutMapping("/{id}")
	@PreAuthorize("hasAuthority('UPDATE') || @postSecurity.isOwner(authentication, #post)")
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	public ResponseEntity<PostInfoWithAuthorAndTags> updatePost(@PathVariable("id") @NonNull Post post,
																@Valid @RequestBody UpdatePostRequest updatePostRequest)
	{
		String title = updatePostRequest.title();
		String body = updatePostRequest.body();
		Set<TagIdOrSlug> tags = updatePostRequest.tags();

		PostInfoWithAuthorAndTags updatedPost = postService.updatePost(post, title, body, tags);

		return ResponseEntity.ok(updatedPost);
	}

	@Operation(summary = "Delete a post", tags = {"posts"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = PostDto.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Post not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
						 content = @Content)
	})
	@DeleteMapping("/{id}")
	@PreAuthorize("hasAuthority('DELETE') || @postSecurity.isOwner(authentication, #post)")
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	public ResponseEntity<PostDto> deletePost(@PathVariable("id") @NonNull Post post)
	{
		postService.deletePost(post.getId());

		return ResponseEntity.ok(new PostDto(post));
	}

	@Operation(summary = "Get comments of a post", tags = {"posts"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json", schema = @Schema(
												implementation = GetPostCommentsResponse.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Post not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
						 content = @Content)
	})
	@GetMapping("/{id}/comments")
	@PreAuthorize("#post.isPublished() || @postSecurity.isOwner(authentication, #post)")
	public ResponseEntity<GetPostCommentsResponse> getPostComments(@PathVariable("id") @NonNull Post post)
	{
		Set<CommentInfo> commentsInfo = commentService.getAllCommentInfoByPostId(post.getId());
		return ResponseEntity.ok(GetPostCommentsResponse.fromCommentsInfo(commentsInfo));
	}

	@Operation(summary = "Create a new comment for a post", tags = {"posts"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = CommentInfo.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Post not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
						 content = @Content)
	})
	@PostMapping("/{id}/comments")
	@PreAuthorize("#post.isPublished() || @postSecurity.isOwner(authentication, #post)")
	public ResponseEntity<CommentInfo> createPostComment(@PathVariable("id") @NonNull Post post, @Valid @RequestBody
	CreatePostCommentRequest createPostCommentRequest)
	{
		String username = createPostCommentRequest.username();
		String body = createPostCommentRequest.body();

		Comment comment = postService.addCommentToPost(post, username, body);
		// Todo : Comment -> CommentInfo without query
		Optional<CommentInfo> commentInfo = commentService.getCommentInfo(comment.getId());
		assert commentInfo.isPresent();

		return ResponseEntity.ok(commentInfo.get());
	}

	@Operation(summary = "Publish a draft post", tags = {"posts"})
	@ApiResponses(value = {@ApiResponse(responseCode = "204", description = "successful operation"),
			@ApiResponse(responseCode = "400", description = "Bad request"),
			@ApiResponse(responseCode = "404", description = "Post not found"),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT"),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions")
	})
	@PostMapping("/{id}/publish")
	@PreAuthorize("hasAuthority('UPDATE') || @postSecurity.isOwner(authentication, #post)")
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	public ResponseEntity<Void> publishPost(@PathVariable("id") @NonNull Post post)
	{
		if (post.isPublished())
		{
			throw PostPublicationConflictException.fromPost(post.getId());
		}

		post = postService.publishPost(post);

		return ResponseEntity.status(HttpStatus.NO_CONTENT)
							 .build();
	}

	@Operation(summary = "Hide a published post", tags = {"posts"})
	@ApiResponses(value = {@ApiResponse(responseCode = "204", description = "successful operation"),
			@ApiResponse(responseCode = "400", description = "Bad request"),
			@ApiResponse(responseCode = "404", description = "Post not found"),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT"),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions")
	})
	@PostMapping("/{id}/hide")
	@PreAuthorize("hasAuthority('UPDATE') || @postSecurity.isOwner(authentication, #post)")
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	public ResponseEntity<Void> hidePost(@PathVariable("id") @NonNull Post post)
	{
		if (!post.isPublished())
		{
			throw PostPublicationConflictException.fromPost(post.getId());
		}

		post = postService.hidePost(post);

		return ResponseEntity.status(HttpStatus.NO_CONTENT)
							 .build();
	}

	@ExceptionHandler(PostPublicationConflictException.class)
	public ProblemDetail handlePostPublicationConflictException(PostPublicationConflictException ex)
	{
		ProblemDetail pd = ProblemDetail.forStatusAndDetail(HttpStatus.CONFLICT, ex.getMessage());
		pd.setTitle("Post publication conflict");
		pd.setType(URI.create("about:blank"));
		pd.setProperty("timestamp",
				OffsetDateTime.now()
							  .toString());
		pd.setProperty("postId", ex.getPostId());
		pd.setProperty("errorCode", "RES_409");

		return pd;
	}
}
