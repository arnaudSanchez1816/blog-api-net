package com.blog.api.apispring.dto.tag;

import com.blog.api.apispring.utils.TagUtils;
import com.fasterxml.jackson.annotation.JsonCreator;
import io.swagger.v3.oas.annotations.media.Schema;

@Schema(description = "Tag identifier: either a numeric ID or a slug string", oneOf = {Long.class, String.class})
public class TagIdOrSlug
{
	@Schema(name = "id", description = "Id of a tag", example = "1")
	private final Long id;
	@Schema(name = "slug", description = "Slug of a tag", example = "javascript")
	private final String slug;

	private TagIdOrSlug(Long id, String slug)
	{
		this.id = id;
		this.slug = slug;
	}

	@JsonCreator
	public static TagIdOrSlug fromString(String value)
	{
		if (value.matches("^\\d+$"))
		{
			return new TagIdOrSlug(Long.parseLong(value), null);
		} else if (value.matches(TagUtils.SLUG_REGEX))
		{
			return new TagIdOrSlug(null, value);
		}

		throw new IllegalArgumentException("Invalid ID or slug format.");
	}

	@JsonCreator(mode = JsonCreator.Mode.DELEGATING)
	public static TagIdOrSlug fromId(long id)
	{
		return new TagIdOrSlug(id, null);
	}

	public static TagIdOrSlug fromSlug(String slug)
	{
		if (slug.matches(TagUtils.SLUG_REGEX))
		{
			return new TagIdOrSlug(null, slug);
		}

		throw new IllegalArgumentException("Invalid slug format.");
	}

	public boolean isId()
	{
		return id != null;
	}

	public boolean isSlug()
	{
		return slug != null;
	}

	public Long getId()
	{
		return id;
	}

	public String getSlug()
	{
		return slug;
	}
}
