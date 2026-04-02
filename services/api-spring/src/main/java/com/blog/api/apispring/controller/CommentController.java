package com.blog.api.apispring.controller;

import com.blog.api.apispring.config.SwaggerConfig;
import com.blog.api.apispring.dto.LoginResponse;
import com.blog.api.apispring.dto.comment.UpdateCommentRequest;
import com.blog.api.apispring.model.Comment;
import com.blog.api.apispring.projection.CommentInfo;
import com.blog.api.apispring.service.CommentService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.validation.Valid;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.*;

import java.util.Optional;

@Tag(name = "comments", description = "Comments related endpoints")
@RestController
@RequestMapping("/comments")
class CommentController
{
	private final CommentService commentService;

	public CommentController(CommentService commentService)
	{
		this.commentService = commentService;
	}

	@Operation(summary = "Get a comment details.", tags = {"comments"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = CommentInfo.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Comment not found", content = @Content)
	})
	@GetMapping("/{id}")
	public ResponseEntity<CommentInfo> getComment(@Valid @PathVariable long id)
	{
		Optional<CommentInfo> optionalCommentInfo = commentService.getCommentInfo(id);
		if (optionalCommentInfo.isEmpty())
		{
			return ResponseEntity.notFound()
								 .build();
		}

		return ResponseEntity.ok(optionalCommentInfo.get());
	}

	@Operation(summary = "Edit a comment.", tags = {"comments"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = CommentInfo.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Comment not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
						 content = @Content)
	})
	@PutMapping("/{id}")
	@PreAuthorize("hasAuthority('UPDATE')")
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	public ResponseEntity<CommentInfo> updateComment(@PathVariable long id,
													 @Valid @RequestBody UpdateCommentRequest request)
	{
		Optional<Comment> optionalComment = commentService.getComment(id);
		if (optionalComment.isEmpty())
		{
			return ResponseEntity.notFound()
								 .build();
		}

		String newUsername = request.username();
		String newBody = request.body();

		Comment comment = optionalComment.get();
		comment = commentService.updateComment(comment, newUsername, newBody);
		// TODO : Comment -> CommentInfo without query ?
		Optional<CommentInfo> commentInfo = commentService.getCommentInfo(comment.getId());
		assert commentInfo.isPresent();

		return ResponseEntity.ok(commentInfo.get());
	}

	@Operation(summary = "Delete a comment.", tags = {"comments"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = CommentInfo.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Comment not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
						 content = @Content)
	})
	@DeleteMapping("/{id}")
	@PreAuthorize("hasAuthority('DELETE')")
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	public ResponseEntity<CommentInfo> deleteComment(@PathVariable long id)
	{
		Optional<CommentInfo> optionalCommentInfo = commentService.getCommentInfo(id);
		if (optionalCommentInfo.isEmpty())
		{
			return ResponseEntity.notFound()
								 .build();
		}

		commentService.deleteCommentById(id);

		return ResponseEntity.ok(optionalCommentInfo.get());
	}
}
