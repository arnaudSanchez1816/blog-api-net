package com.blog.api.apispring.controller;

import com.blog.api.apispring.config.SwaggerConfig;
import com.blog.api.apispring.dto.tag.CreateTagRequest;
import com.blog.api.apispring.dto.tag.GetTagsResponse;
import com.blog.api.apispring.dto.tag.TagIdOrSlug;
import com.blog.api.apispring.dto.tag.UpdateTagRequest;
import com.blog.api.apispring.dto.users.UserDetailsDto;
import com.blog.api.apispring.model.Tag;
import com.blog.api.apispring.service.TagService;
import com.blog.api.apispring.utils.TagUtils;
import com.blog.api.apispring.validation.TagSlug;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Pattern;
import org.jspecify.annotations.NonNull;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.util.UriComponentsBuilder;

import java.net.URI;
import java.util.List;
import java.util.Optional;
import java.util.function.Function;

@io.swagger.v3.oas.annotations.tags.Tag(name = "tags", description = "Tags related endpoints")
@RestController
@RequestMapping("/tags")
public class TagController
{
	private final TagService tagService;

	public TagController(TagService tagService)
	{
		this.tagService = tagService;
	}

	@Operation(summary = "Get all post tags.", tags = {"tags"})
	@ApiResponses(value = @ApiResponse(responseCode = "200", description = "successful operation",
									   content = @Content(mediaType = "application/json",
														  schema = @Schema(implementation = GetTagsResponse.class))))
	@GetMapping
	public ResponseEntity<GetTagsResponse> getTags()
	{
		List<Tag> tags = tagService.getAllTags();
		return ResponseEntity.ok(new GetTagsResponse(tags));
	}

	@Operation(summary = "Get all post tags.", tags = {"tags"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = GetTagsResponse.class))),
			@ApiResponse(responseCode = "400", description = "Invalid tag id or slug", content = @Content),
			@ApiResponse(responseCode = "404", description = "Tag not found", content = @Content)
	})
	@GetMapping("/{idOrSlug}")
	public ResponseEntity<Tag> getTag(
			@Parameter(required = true, description = "Either a tag id or a tag slug.") @PathVariable
			TagIdOrSlug idOrSlug)
	{
		return handleTagIdOrSlug(idOrSlug, id ->
		{
			Optional<Tag> optionalTag = tagService.getTag(id);
			return optionalTag.map(ResponseEntity::ok)
							  .orElseGet(() -> ResponseEntity.notFound()
															 .build());
		}, slug ->
		{
			Optional<Tag> optionalTag = tagService.getTag(slug);
			return optionalTag.map(ResponseEntity::ok)
							  .orElseGet(() -> ResponseEntity.notFound()
															 .build());
		});
	}

	@Operation(summary = "Create a new post tag.", tags = {"tags"})
	@ApiResponses(value = {@ApiResponse(responseCode = "201", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = Tag.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
						 content = @Content)
	})
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	@PostMapping(produces = "application/json")
	public ResponseEntity<Tag> createTag(@Valid @RequestBody CreateTagRequest createTagDto, UriComponentsBuilder ucb)
	{
		Tag newTag = tagService.createTag(createTagDto);
		URI tagLocation = ucb.path("/tags/{id}")
							 .buildAndExpand(newTag.getId())
							 .toUri();
		return ResponseEntity.created(tagLocation)
							 .body(newTag);
	}

	@Operation(summary = "Edit a post tag.", tags = {"tags"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = Tag.class))),
			@ApiResponse(responseCode = "400", description = "Invalid tag id or slug", content = @Content),
			@ApiResponse(responseCode = "404", description = "Tag not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
						 content = @Content)
	})
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	@PutMapping("/{idOrSlug}")
	public ResponseEntity<Tag> updateTag(
			@Parameter(description = "Either a tag id or slug") @PathVariable TagIdOrSlug idOrSlug,
			@Valid @RequestBody UpdateTagRequest updateTagDto)
	{
		return handleTagIdOrSlug(idOrSlug, id ->
		{
			Optional<Tag> tag = tagService.getTag(id);
			if (tag.isEmpty())
			{
				return ResponseEntity.notFound()
									 .build();
			}
			Tag updatedTag = tagService.updateTag(id, updateTagDto);
			return ResponseEntity.ok(updatedTag);
		}, slug ->
		{
			Optional<Tag> tag = tagService.getTag(slug);
			if (tag.isEmpty())
			{
				return ResponseEntity.notFound()
									 .build();
			}
			Tag updatedTag = tagService.updateTag(tag.get()
													 .getId(), updateTagDto);
			return ResponseEntity.ok(updatedTag);
		});
	}

	@Operation(summary = "Delete a post tag.", tags = {"tags"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = Tag.class))),
			@ApiResponse(responseCode = "400", description = "Invalid tag id or slug", content = @Content),
			@ApiResponse(responseCode = "404", description = "Tag not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
						 content = @Content)
	})
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	@DeleteMapping("/{idOrSlug}")
	public ResponseEntity<Tag> deleteTag(
			@Parameter(description = "Either a tag id or slug") @PathVariable TagIdOrSlug idOrSlug)
	{
		return handleTagIdOrSlug(idOrSlug, id ->
		{
			Optional<Tag> deletedTag = tagService.deleteTag(id);
			return deletedTag.map(ResponseEntity::ok)
							 .orElseGet(() -> ResponseEntity.notFound()
															.build());
		}, slug ->
		{
			Optional<Tag> deletedTag = tagService.deleteTag(slug);
			return deletedTag.map(ResponseEntity::ok)
							 .orElseGet(() -> ResponseEntity.notFound()
															.build());
		});
	}

	private static <T> ResponseEntity<T> handleTagIdOrSlug(TagIdOrSlug idOrSlug,
														   Function<Long, ResponseEntity<T>> idHandler,
														   Function<String, ResponseEntity<T>> slugHandler)
	{
		if (idOrSlug.isId())
		{
			return idHandler.apply(idOrSlug.getId());
		} else
		{
			return slugHandler.apply(idOrSlug.getSlug());
		}
	}
}
