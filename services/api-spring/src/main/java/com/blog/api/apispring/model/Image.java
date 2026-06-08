package com.blog.api.apispring.model;

import jakarta.persistence.*;
import jakarta.validation.constraints.NotNull;
import lombok.Getter;
import lombok.Setter;
import org.hibernate.annotations.OnDelete;
import org.hibernate.annotations.OnDeleteAction;
import org.hibernate.validator.constraints.URL;

import java.util.UUID;

@Table(name = "images")
@Entity
@Getter
@Setter
public class Image
{

	@Id
	@GeneratedValue(strategy = GenerationType.UUID)
	private UUID id;

	@NotNull
	@URL
	private String url;

	@ManyToOne(optional = false)
	@JoinColumn(name = "post_id", nullable = false, updatable = false)
	@OnDelete(action = OnDeleteAction.CASCADE)
	private Post post;
}
