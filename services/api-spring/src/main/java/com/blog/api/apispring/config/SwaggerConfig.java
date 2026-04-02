package com.blog.api.apispring.config;

import io.swagger.v3.oas.annotations.enums.SecuritySchemeType;
import io.swagger.v3.oas.annotations.security.SecurityScheme;
import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Info;
import org.springdoc.core.models.GroupedOpenApi;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
@SecurityScheme(name = SwaggerConfig.JWT_SECURITY_SCHEME, type = SecuritySchemeType.HTTP, bearerFormat = "JWT",
				scheme = "bearer",
				description = "Obtain a valid token by using the /auth/login endpoint. Default user credentials details are available in application.properties")
public class SwaggerConfig
{
	public static final String JWT_SECURITY_SCHEME = "Bearer Authentication";

	@Bean
	public GroupedOpenApi publicApi()
	{
		return GroupedOpenApi.builder()
							 .group("public")
							 .pathsToMatch("/**")
							 .build();
	}

	@Bean
	public OpenAPI openAPI()
	{
		return new OpenAPI().info(new Info().title("Blog-api")
											.description("Rest API of the Blog-api platform.")
											.version("1.0"));
	}
}
