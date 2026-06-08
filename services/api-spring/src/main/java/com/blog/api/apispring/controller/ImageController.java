package com.blog.api.apispring.controller;

import com.blog.api.apispring.config.SwaggerConfig;
import com.blog.api.apispring.model.Image;
import com.blog.api.apispring.projection.CommentInfo;
import com.blog.api.apispring.service.ImageService;
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
import java.util.UUID;

@Tag(name = "images", description = "Images related endpoints")
@RestController
@RequestMapping("/images")
class ImageController
{
	private final ImageService imageService;

	public ImageController(ImageService imageService)
	{
		this.imageService = imageService;
	}

	@Operation(summary = "Get a post image details.", tags = {"images"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "success",
	                                    content = @Content(mediaType = "application/json",
	                                                       schema = @Schema(implementation = Image.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Image not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
			             content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
			             content = @Content)
	})
	@GetMapping("/{id}")
	@PreAuthorize("hasAuthority('READ')")
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	public ResponseEntity<Image> getImage(@Valid @PathVariable UUID id)
	{
		Optional<Image> optionalImage = imageService.getImage(id);
		if (optionalImage.isEmpty())
		{
			return ResponseEntity.notFound()
			                     .build();
		}
		return ResponseEntity.ok(optionalImage.get());
	}

	@Operation(summary = "Delete a post image.", tags = {"images"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "success",
	                                    content = @Content(mediaType = "application/json",
	                                                       schema = @Schema(implementation = Image.class))),
			@ApiResponse(responseCode = "400", description = "Bad request", content = @Content),
			@ApiResponse(responseCode = "404", description = "Image not found", content = @Content),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
			             content = @Content),
			@ApiResponse(responseCode = "403", description = "Authenticated user is missing permissions",
			             content = @Content)
	})
	@DeleteMapping("/{id}")
	@PreAuthorize("hasAuthority('DELETE')")
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	public ResponseEntity<Image> deleteImage(@Valid @PathVariable UUID id)
	{
		Optional<Image> optionalImage = imageService.getImage(id);
		if (optionalImage.isEmpty())
		{
			return ResponseEntity.notFound()
			                     .build();
		}
		imageService.deleteImageById(id);
		return ResponseEntity.ok(optionalImage.get());
	}
}
