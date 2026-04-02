package com.blog.api.apispring.config;

import com.blog.api.apispring.exception.handler.RestAccessDeniedHandler;
import com.blog.api.apispring.exception.handler.RestAuthenticationEntryPoint;
import com.blog.api.apispring.security.authorities.PermissionType;
import com.blog.api.apispring.security.filter.AccessJwtAuthenticationFilter;
import com.blog.api.apispring.security.filter.RefreshJwtAuthenticationFilter;
import com.blog.api.apispring.security.provider.JwtAuthenticationProvider;
import com.blog.api.apispring.security.userdetails.service.BlogUserDetailsService;
import com.blog.api.apispring.service.JwtService;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.env.Environment;
import org.springframework.http.HttpMethod;
import org.springframework.security.access.hierarchicalroles.RoleHierarchy;
import org.springframework.security.access.hierarchicalroles.RoleHierarchyImpl;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.ProviderManager;
import org.springframework.security.authentication.dao.DaoAuthenticationProvider;
import org.springframework.security.authorization.AuthorizationManagerFactory;
import org.springframework.security.authorization.DefaultAuthorizationManagerFactory;
import org.springframework.security.config.annotation.method.configuration.EnableMethodSecurity;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.authentication.UsernamePasswordAuthenticationFilter;
import org.springframework.web.cors.CorsConfiguration;
import org.springframework.web.cors.UrlBasedCorsConfigurationSource;
import org.springframework.web.filter.CorsFilter;

import java.util.List;

@Configuration
@EnableWebSecurity
@EnableMethodSecurity
@ConditionalOnProperty(name = "blog-api.security.enable", havingValue = "true")
public class SecurityConfig
{
	private final RestAuthenticationEntryPoint restAuthenticationEntryPoint;
	private final RestAccessDeniedHandler restAccessDeniedHandler;
	private final JwtService jwtService;
	private final Environment environment;

	public SecurityConfig(RestAuthenticationEntryPoint restAuthenticationEntryPoint,
						  RestAccessDeniedHandler restAccessDeniedHandler, JwtService jwtService,
						  Environment environment)
	{
		this.restAuthenticationEntryPoint = restAuthenticationEntryPoint;
		this.restAccessDeniedHandler = restAccessDeniedHandler;
		this.jwtService = jwtService;
		this.environment = environment;
	}

	@Bean
	public SecurityFilterChain web(HttpSecurity http, BlogUserDetailsService userDetailsService) throws Exception
	{
		http.userDetailsService(userDetailsService);
		http.csrf(AbstractHttpConfigurer::disable);
		http.sessionManagement(sm -> sm.sessionCreationPolicy(SessionCreationPolicy.STATELESS));
		http.httpBasic(AbstractHttpConfigurer::disable);
		http.formLogin(AbstractHttpConfigurer::disable);
		http.authorizeHttpRequests(authorize ->
		{
			authorize.requestMatchers(HttpMethod.POST, "/auth/login")
					 .permitAll();
			authorize.requestMatchers(HttpMethod.GET, "/auth/token")
					 .authenticated();
			// Users route
			authorize.requestMatchers(HttpMethod.GET, "/users/**")
					 .authenticated();
			// Tags route
			authorize.requestMatchers(HttpMethod.GET, "/tags/*")
					 .permitAll();
			authorize.requestMatchers(HttpMethod.POST, "/tags/**")
					 .hasAuthority(PermissionType.CREATE.name());
			authorize.requestMatchers(HttpMethod.DELETE, "/tags/**")
					 .hasAuthority(PermissionType.DELETE.name());
			authorize.requestMatchers(HttpMethod.PUT, "/tags/**")
					 .hasAuthority(PermissionType.UPDATE.name());
			// Posts route
			authorize.requestMatchers("/posts/*/comments")
					 .permitAll();
			authorize.requestMatchers(HttpMethod.GET, "/posts/**")
					 .permitAll();
			authorize.requestMatchers("/posts/**")
					 .authenticated();
			// Comments routes
			// Authority check done with MethodSecutiry
			authorize.requestMatchers(HttpMethod.GET, "/comments/*")
					 .permitAll()
					 .requestMatchers("/comments/**")
					 .authenticated();
			// Any
			authorize.anyRequest()
					 .permitAll();
		});

		// === Error handling JSON ===
		http.exceptionHandling(ex ->
		{
			ex.authenticationEntryPoint(restAuthenticationEntryPoint)
			  .accessDeniedHandler(restAccessDeniedHandler);
		});

		// === JWT Filters ===
		AuthenticationManager authenticationManager = authenticationManager(userDetailsService);
		http.addFilterBefore(new RefreshJwtAuthenticationFilter(jwtService, authenticationManager),
				UsernamePasswordAuthenticationFilter.class);
		http.addFilterBefore(new AccessJwtAuthenticationFilter(jwtService, authenticationManager),
				RefreshJwtAuthenticationFilter.class);
		return http.build();
	}

	@Bean
	public AuthenticationManager authenticationManager(BlogUserDetailsService userDetailsService)
	{
		DaoAuthenticationProvider usernamePasswordAuthProvider = new DaoAuthenticationProvider(userDetailsService);
		usernamePasswordAuthProvider.setPasswordEncoder(passwordEncoder());
		JwtAuthenticationProvider jwtAuthenticationProvider = new JwtAuthenticationProvider(userDetailsService);

		return new ProviderManager(usernamePasswordAuthProvider, jwtAuthenticationProvider);
	}

	@Bean
	<T> AuthorizationManagerFactory<T> authorizationManagerFactory()
	{
		DefaultAuthorizationManagerFactory<T> authorizationManagerFactory = new DefaultAuthorizationManagerFactory<>();
		authorizationManagerFactory.setRoleHierarchy(roleHierarchy());

		return authorizationManagerFactory;
	}

	@Bean
	public RoleHierarchy roleHierarchy()
	{
		return RoleHierarchyImpl.withDefaultRolePrefix()
								.role("ADMIN")
								.implies("USER")
								.build();
	}

	@Bean
	public PasswordEncoder passwordEncoder()
	{
		return new BCryptPasswordEncoder();
	}

	//	// === CORS bean ===
	@Bean
	public UrlBasedCorsConfigurationSource corsConfigurationSource()
	{
		CorsConfiguration cfg = new CorsConfiguration();

		cfg.setAllowCredentials(true);
		cfg.setAllowedMethods(List.of("GET", "POST", "PUT", "DELETE", "OPTIONS"));
		String corsAllowedUrls = environment.getRequiredProperty("blog-api.security.cors-allowed-urls");
		String[] urls = corsAllowedUrls.split(",");
		for (String url : urls)
		{
			cfg.addAllowedOriginPattern(url);
		}
		cfg.setAllowedHeaders(List.of("Authorization", "Content-Type", "Accept"));
		cfg.setExposedHeaders(List.of("Authorization"));
		// Preflight cache
		cfg.setMaxAge(3600L);

		UrlBasedCorsConfigurationSource src = new UrlBasedCorsConfigurationSource();
		src.registerCorsConfiguration("/**", cfg);
		return src;
	}
}
