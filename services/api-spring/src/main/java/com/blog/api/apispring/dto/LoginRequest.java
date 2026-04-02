package com.blog.api.apispring.dto;

import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.constraints.NotBlank;

public record LoginRequest(@Schema(requiredMode = Schema.RequiredMode.REQUIRED)
						   @NotBlank
						   String email,
						   @Schema(requiredMode = Schema.RequiredMode.REQUIRED)
						   @NotBlank
						   String password)
{
}
