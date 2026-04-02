package com.blog.api.apispring.dto.users;

import com.blog.api.apispring.dto.posts.GetPostsRequest;
import com.blog.api.apispring.dto.posts.GetPostsRequestImpl;
import com.blog.api.apispring.dto.tag.TagIdOrSlug;
import com.blog.api.apispring.enums.PostSortBy;
import com.blog.api.apispring.model.Post;
import com.blog.api.apispring.specs.PostSpecs;
import io.swagger.v3.oas.annotations.Parameter;
import io.swagger.v3.oas.annotations.media.Schema;
import jakarta.validation.Valid;
import org.springframework.data.domain.Pageable;
import org.springframework.data.jpa.domain.Specification;

import java.util.Collection;

public class GetUserPostsRequest implements GetPostsRequest
{
	@Valid
	private final GetPostsRequestImpl getPostsRequest = new GetPostsRequestImpl();

	@Override
	public String getQ()
	{
		return this.getPostsRequest.getQ();
	}

	@Override
	public void setQ(String q)
	{
		this.getPostsRequest.setQ(q);
	}

	@Override
	public int getPage()
	{
		return this.getPostsRequest.getPage();
	}

	@Override
	public void setPage(int page)
	{
		this.getPostsRequest.setPage(page);
	}

	@Override
	public int getPageSize()
	{
		return this.getPostsRequest.getPageSize();
	}

	@Override
	public void setPageSize(int pageSize)
	{
		this.getPostsRequest.setPageSize(pageSize);
	}

	@Override
	public PostSortBy getSortBy()
	{
		return this.getPostsRequest.getSortBy();
	}

	@Override
	public void setSortBy(PostSortBy sortBy)
	{
		this.getPostsRequest.setSortBy(sortBy);
	}

	@Override
	public Collection<TagIdOrSlug> getTags()
	{
		return this.getPostsRequest.getTags();
	}

	@Override
	public void setTags(Collection<TagIdOrSlug> tags)
	{
		this.getPostsRequest.setTags(tags);
	}

	@Schema(hidden = true)
	@Override
	public boolean isUnpublished()
	{
		return true;
	}

	@Override
	public void setUnpublished(boolean val)
	{
	}

	@Override
	public Pageable toPageable()
	{
		return this.getPostsRequest.toPageable();
	}

	@Override
	public Specification<Post> toSpecifications()
	{
		String q = this.getQ();
		Collection<TagIdOrSlug> tags = this.getTags();

		return PostSpecs.withTags(tags)
						.and(PostSpecs.titleContains(q))
						.and(PostSpecs.onlyPublished(false));
	}
}
