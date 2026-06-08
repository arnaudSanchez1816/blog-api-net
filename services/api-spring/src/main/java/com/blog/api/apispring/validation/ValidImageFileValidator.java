package com.blog.api.apispring.validation;

import com.blog.api.apispring.service.ImageService;
import jakarta.validation.ConstraintValidator;
import jakarta.validation.ConstraintValidatorContext;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.web.multipart.MultipartFile;

@Slf4j
public class ValidImageFileValidator implements ConstraintValidator<ValidImageFile, MultipartFile>
{
	private final ImageService imageService;

	// Update the default message in ValidImageFile annotation if size change.
	public static final int MAX_IMAGE_SIZE = 2097152;

	@Autowired
	public ValidImageFileValidator(ImageService imageService)
	{
		this.imageService = imageService;
	}

	@Override
	public boolean isValid(MultipartFile value, ConstraintValidatorContext context)
	{
		log.debug("Validate Image File Type");
		boolean imageIsSupported = this.imageService.isSupportedContentType(value);

		if (imageIsSupported == false)
		{
			return false;
		}

		long size = value.getSize();
		if (size > MAX_IMAGE_SIZE)
		{
			return false;
		}

		return true;
	}
}
