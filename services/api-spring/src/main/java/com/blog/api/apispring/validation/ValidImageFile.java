package com.blog.api.apispring.validation;

import jakarta.validation.Constraint;
import jakarta.validation.Payload;

import java.lang.annotation.*;

@Documented
@Target({ElementType.FIELD, ElementType.PARAMETER})
@Retention(RetentionPolicy.RUNTIME)
@Constraint(validatedBy = TagSlugImpl.class)
public @interface ValidImageFile
{
	String message() default "Invalid image file. Supported types are jpg, png, wepb and avif. Max image size is 2 MB.";

	Class<?>[] groups() default {};

	Class<? extends Payload>[] payload() default {};
}
