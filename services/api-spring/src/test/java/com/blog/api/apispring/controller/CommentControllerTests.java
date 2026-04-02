package com.blog.api.apispring.controller;

import com.blog.api.apispring.PostgresTestConfig;
import com.blog.api.apispring.extensions.ClearDatabaseExtension;
import com.blog.api.apispring.model.Comment;
import com.blog.api.apispring.model.Post;
import com.blog.api.apispring.model.User;
import com.blog.api.apispring.repository.CommentRepository;
import com.blog.api.apispring.repository.PostRepository;
import com.blog.api.apispring.repository.UserRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.webmvc.test.autoconfigure.AutoConfigureMockMvc;
import org.springframework.context.annotation.Import;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.security.test.context.support.WithMockUser;
import org.springframework.test.context.web.WebAppConfiguration;
import org.springframework.test.web.servlet.assertj.MockMvcTester;
import org.springframework.test.web.servlet.assertj.MvcTestResult;

import java.time.OffsetDateTime;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
@Import(PostgresTestConfig.class)
@WebAppConfiguration
@AutoConfigureMockMvc
@ExtendWith(ClearDatabaseExtension.class)
class CommentControllerTests
{
	@Autowired
	private MockMvcTester mockMvc;

	@Autowired
	private UserRepository userRepository;

	@Autowired
	private PostRepository postRepository;

	@Autowired
	private CommentRepository commentRepository;

	private Post post;

	@BeforeEach
	void setUp()
	{
		User author = new User("author@blog.com", "Test Author", "password");
		author = userRepository.save(author);

		post = new Post();
		post.setTitle("First Post");
		post.setDescription("First post description");
		post.setBody("First post body content");
		post.setReadingTime(5);
		post.setPublishedAt(OffsetDateTime.now());
		post.setAuthor(author);
		post = postRepository.save(post);
	}

	@Nested
	@DisplayName("getComment")
	class GetComment
	{
		@Test
		@WithMockUser(username = "Admin")
		void getComment_IsOk_WhenGivenValidCommentId()
		{
			OffsetDateTime now = OffsetDateTime.now();
			Comment comment = new Comment();
			comment.setBody("Body");
			comment.setUsername("Username");
			comment.setCreatedAt(now);
			comment.setPost(post);
			comment = commentRepository.save(comment);

			MvcTestResult response = mockMvc.get()
											.contentType(MediaType.APPLICATION_JSON)
											.uri("/comments/" + comment.getId())
											.exchange();
			Comment finalComment = comment;
			assertThat(response).hasStatusOk()
								.hasContentType(MediaType.APPLICATION_JSON)
								.bodyJson()
								.satisfies(json ->
								{
									json.assertThat()
										.extractingPath("$.id")
										.convertTo(Long.class)
										.isEqualTo(finalComment.getId());
									json.assertThat()
										.extractingPath("$.body")
										.isEqualTo("Body");
									json.assertThat()
										.extractingPath("$.username")
										.isEqualTo("Username");
									json.assertThat()
										.extractingPath("$.createdAt")
										.isNotNull();
									json.assertThat()
										.extractingPath("$.postId")
										.convertTo(Long.class)
										.isEqualTo(post.getId());
								});
		}

		@Test
		@WithMockUser(username = "Admin")
		void getComment_Is404_WhenWrongCommentId()
		{
			MvcTestResult response = mockMvc.get()
											.contentType(MediaType.APPLICATION_JSON)
											.uri("/comments/" + 9999L)
											.exchange();
			assertThat(response).hasStatus(HttpStatus.NOT_FOUND);
		}
	}

	@Nested
	@DisplayName("deleteComment")
	class DeleteComment
	{
		@Test
		@WithMockUser(username = "Admin", authorities = "DELETE")
		void deleteComment_IsOk_WhenGivenValidCommentId()
		{
			OffsetDateTime now = OffsetDateTime.now();
			Comment comment = new Comment();
			comment.setBody("Body");
			comment.setUsername("Username");
			comment.setCreatedAt(now);
			comment.setPost(post);
			comment = commentRepository.save(comment);

			MvcTestResult response = mockMvc.delete()
											.contentType(MediaType.APPLICATION_JSON)
											.uri("/comments/" + comment.getId())
											.exchange();
			Comment finalComment = comment;
			assertThat(response).hasStatusOk()
								.hasContentType(MediaType.APPLICATION_JSON)
								.bodyJson()
								.satisfies(json ->
								{
									json.assertThat()
										.extractingPath("$.id")
										.convertTo(Long.class)
										.isEqualTo(finalComment.getId());
									json.assertThat()
										.extractingPath("$.body")
										.isEqualTo("Body");
									json.assertThat()
										.extractingPath("$.username")
										.isEqualTo("Username");
									json.assertThat()
										.extractingPath("$.createdAt")
										.isNotNull();
									json.assertThat()
										.extractingPath("$.postId")
										.convertTo(Long.class)
										.isEqualTo(post.getId());
								});

			Optional<Comment> deletedComment = commentRepository.findById(finalComment.getId());
			assertThat(deletedComment).isEmpty();
		}

		@Test
		@WithMockUser(username = "Admin", authorities = "DELETE")
		void deleteComment_Is404_WhenWrongCommentId()
		{
			MvcTestResult response = mockMvc.delete()
											.contentType(MediaType.APPLICATION_JSON)
											.uri("/comments/" + 9999L)
											.exchange();
			assertThat(response).hasStatus(HttpStatus.NOT_FOUND);
		}

		@Test
		void deleteComment_Is401_WhenUnauthenticated()
		{
			MvcTestResult response = mockMvc.delete()
											.contentType(MediaType.APPLICATION_JSON)
											.uri("/comments/" + 1L)
											.exchange();
			assertThat(response).hasStatus(HttpStatus.UNAUTHORIZED);
		}

		@Test
		@WithMockUser(username = "User")
		void deleteComment_Is403_WhenUserLacksDeleteAuthority()
		{
			MvcTestResult response = mockMvc.delete()
											.contentType(MediaType.APPLICATION_JSON)
											.uri("/comments/" + 1L)
											.exchange();
			assertThat(response).hasStatus(HttpStatus.FORBIDDEN);
		}
	}

	@Nested
	@DisplayName("updateComment")
	class UpdateComment
	{
		@Test
		@WithMockUser(username = "Admin", authorities = "UPDATE")
		void updateComment_IsOk_WhenGivenValidCommentIdAndRequest()
		{
			OffsetDateTime now = OffsetDateTime.now();
			Comment comment = new Comment();
			comment.setBody("Original Body");
			comment.setUsername("Original Username");
			comment.setCreatedAt(now);
			comment.setPost(post);
			comment = commentRepository.save(comment);

			String requestBody = """
					{
						"username": "Updated Username",
						"body": "Updated Body"
					}
					""";

			MvcTestResult response = mockMvc.put()
											.contentType(MediaType.APPLICATION_JSON)
											.uri("/comments/" + comment.getId())
											.content(requestBody)
											.exchange();
			Comment finalComment = comment;
			assertThat(response).hasStatusOk()
								.hasContentType(MediaType.APPLICATION_JSON)
								.bodyJson()
								.satisfies(json ->
								{
									json.assertThat()
										.extractingPath("$.id")
										.convertTo(Long.class)
										.isEqualTo(finalComment.getId());
									json.assertThat()
										.extractingPath("$.body")
										.isEqualTo("Updated Body");
									json.assertThat()
										.extractingPath("$.username")
										.isEqualTo("Updated Username");
									json.assertThat()
										.extractingPath("$.createdAt")
										.isNotNull();
									json.assertThat()
										.extractingPath("$.postId")
										.convertTo(Long.class)
										.isEqualTo(post.getId());
								});

			Optional<Comment> updatedComment = commentRepository.findById(finalComment.getId());
			assertThat(updatedComment).isPresent();
			assertThat(updatedComment.get()
									 .getBody()).isEqualTo("Updated Body");
			assertThat(updatedComment.get()
									 .getUsername()).isEqualTo("Updated Username");
		}

		@Test
		@WithMockUser(username = "Admin", authorities = "UPDATE")
		void updateComment_Is404_WhenWrongCommentId()
		{
			String requestBody = """
					{
						"username": "Updated Username",
						"body": "Updated Body"
					}
					""";

			MvcTestResult response = mockMvc.put()
											.contentType(MediaType.APPLICATION_JSON)
											.uri("/comments/" + 9999L)
											.content(requestBody)
											.exchange();
			assertThat(response).hasStatus(HttpStatus.NOT_FOUND);
		}

		@Test
		void updateComment_Is401_WhenUnauthenticated()
		{
			String requestBody = """
					{
						"username": "Updated Username",
						"body": "Updated Body"
					}
					""";

			MvcTestResult response = mockMvc.put()
											.contentType(MediaType.APPLICATION_JSON)
											.uri("/comments/" + 1L)
											.content(requestBody)
											.exchange();
			assertThat(response).hasStatus(HttpStatus.UNAUTHORIZED);
		}

		@Test
		@WithMockUser(username = "User")
		void updateComment_Is403_WhenUserLacksUpdateAuthority()
		{
			String requestBody = """
					{
						"username": "Updated Username",
						"body": "Updated Body"
					}
					""";

			MvcTestResult response = mockMvc.put()
											.contentType(MediaType.APPLICATION_JSON)
											.uri("/comments/" + 1L)
											.content(requestBody)
											.exchange();
			assertThat(response).hasStatus(HttpStatus.FORBIDDEN);
		}
	}
}