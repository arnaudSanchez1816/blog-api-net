package com.blog.api.apispring.controller;

import com.blog.api.apispring.config.SwaggerConfig;
import com.blog.api.apispring.dto.GetTokenResponse;
import com.blog.api.apispring.dto.LoginRequest;
import com.blog.api.apispring.dto.LoginResponse;
import com.blog.api.apispring.dto.users.UserDetailsDto;
import com.blog.api.apispring.security.userdetails.BlogUserDetails;
import com.blog.api.apispring.service.JwtService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.media.Content;
import io.swagger.v3.oas.annotations.media.Schema;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import io.swagger.v3.oas.annotations.responses.ApiResponses;
import io.swagger.v3.oas.annotations.security.SecurityRequirement;
import io.swagger.v3.oas.annotations.tags.Tag;
import jakarta.servlet.ServletContext;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.web.bind.annotation.*;

import static com.blog.api.apispring.security.filter.RefreshJwtAuthenticationFilter.REFRESH_TOKEN_COOKIE;

@Tag(name = "auth", description = "Authentication related endpoints.")
@RestController
@RequestMapping("/auth")
class AuthController
{
	private final AuthenticationManager authenticationManager;
	private final JwtService jwtService;
	private final ServletContext servletContext;

	// 30 days
	private static final int REFRESH_COOKIE_MAX_AGE = 30 * 24 * 60 * 60;

	public AuthController(AuthenticationManager authenticationManager, JwtService jwtService,
						  ServletContext servletContext)
	{
		this.authenticationManager = authenticationManager;
		this.jwtService = jwtService;
		this.servletContext = servletContext;
	}

	@Operation(summary = "Login. Set a http-only refresh JWT cookie and returns user details and a JWT access token.",
			   tags = {"auth"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = LoginResponse.class))),
			@ApiResponse(responseCode = "401", description = "Invalid email/password supplied.", content = @Content)
	})
	@PostMapping("/login")
	public ResponseEntity<LoginResponse> login(@RequestBody LoginRequest loginRequest, HttpServletResponse response)
	{
		Authentication authenticationRequest = UsernamePasswordAuthenticationToken.unauthenticated(loginRequest.email(),
				loginRequest.password());
		Authentication authenticationResponse = this.authenticationManager.authenticate(authenticationRequest);
		BlogUserDetails userDetails = (BlogUserDetails) authenticationResponse.getPrincipal();
		if (userDetails == null)
		{
			throw new RuntimeException("Unexpected User details type");
		}

		// Generate refresh token
		Long userId = userDetails.getId();
		String username = userDetails.getUsername();
		String email = userDetails.getEmail();
		String refreshToken = jwtService.generateRefreshToken(userId, username, email);

		Cookie refreshTokenCookie = generateRefreshTokenCookie(refreshToken, REFRESH_COOKIE_MAX_AGE);
		response.addCookie(refreshTokenCookie);

		// Generate access token
		String accessToken = generateAccessToken(userDetails);

		return ResponseEntity.ok(new LoginResponse(accessToken, UserDetailsDto.fromBlogUserDetails(userDetails)));
	}

	@Operation(summary = "Logout the current user, clears the http-only Refresh token cookie.", tags = {"auth"})
	@ApiResponses(value = @ApiResponse(responseCode = "200", description = "successful operation"))
	@GetMapping("/logout")
	public ResponseEntity<Void> logout(HttpServletResponse response)
	{
		// Delete the refresh token cookie by setting maxAge to 0
		Cookie expiredRefreshTokenCookie = generateRefreshTokenCookie("", 0);
		response.addCookie(expiredRefreshTokenCookie);

		return ResponseEntity.ok()
							 .build();
	}

	private Cookie generateRefreshTokenCookie(String value, int maxAge)
	{
		Cookie refreshTokenCookie = new Cookie(REFRESH_TOKEN_COOKIE, value);
		refreshTokenCookie.setHttpOnly(true);
		refreshTokenCookie.setSecure(true);
		refreshTokenCookie.setMaxAge(maxAge);
		refreshTokenCookie.setPath(servletContext.getContextPath() + "/auth/token");
		refreshTokenCookie.setAttribute("SameSite", "Strict");
		return refreshTokenCookie;
	}

	@Operation(summary = "Generate and return a new JWT access token for the authenticated user.", tags = {"auth"})
	@ApiResponses(value = {@ApiResponse(responseCode = "200", description = "successful operation",
										content = @Content(mediaType = "application/json",
														   schema = @Schema(implementation = GetTokenResponse.class))),
			@ApiResponse(responseCode = "401", description = "Missing or invalid authentication JWT",
						 content = @Content)
	})
	@SecurityRequirement(name = SwaggerConfig.JWT_SECURITY_SCHEME)
	@GetMapping("/token")
	public ResponseEntity<GetTokenResponse> getToken(@AuthenticationPrincipal BlogUserDetails userDetails)
	{
		String accessToken = generateAccessToken(userDetails);

		return ResponseEntity.ok(new GetTokenResponse(accessToken));
	}

	private String generateAccessToken(BlogUserDetails userDetails)
	{
		Long userId = userDetails.getId();
		String username = userDetails.getUsername();
		String email = userDetails.getEmail();
		return jwtService.generateAccessToken(userId, username, email);
	}
}
