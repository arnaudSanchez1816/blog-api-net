package com.blog.api.apispring.dto.posts;

import com.blog.api.apispring.dto.tag.TagIdOrSlug;
import com.blog.api.apispring.enums.PostSortBy;
import com.blog.api.apispring.model.Post;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.media.Schema;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.domain.Specification;

import java.util.Collection;

public interface GetPostsRequest
{
	@Schema(requiredMode = Schema.RequiredMode.NOT_REQUIRED, description = "Title of the post to use as filter.")
	String getQ();

	void setQ(String q);

	@Schema(requiredMode = Schema.RequiredMode.NOT_REQUIRED, description = "Page number to fetch. 0-based.")
	int getPage();

	void setPage(int page);

	@Schema(requiredMode = Schema.RequiredMode.NOT_REQUIRED, description = "Size of the page. Capped to 50 items.")
	int getPageSize();

	void setPageSize(int pageSize);

	@Schema(requiredMode = Schema.RequiredMode.NOT_REQUIRED, description = "Sort order used to sort posts.")
	PostSortBy getSortBy();

	void setSortBy(PostSortBy sortBy);

	@Schema(requiredMode = Schema.RequiredMode.NOT_REQUIRED, description = "Filter by posts by tags.")
	Collection<TagIdOrSlug> getTags();

	void setTags(Collection<TagIdOrSlug> tags);

	@Schema(requiredMode = Schema.RequiredMode.NOT_REQUIRED, description = "Show unpublished posts or not.")
	boolean isUnpublished();

	void setUnpublished(boolean isUnpublished);

	Pageable toPageable();

	Specification<Post> toSpecifications();
}
