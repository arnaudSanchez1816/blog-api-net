package com.blog.api.apispring.service;

import com.blog.api.apispring.model.Image;
import com.blog.api.apispring.model.Post;
import com.blog.api.apispring.repository.ImageRepository;
import jakarta.validation.constraints.NotNull;
import org.apache.tika.Tika;
import org.springframework.core.io.Resource;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;
import java.util.Optional;
import java.util.UUID;

@Service
public class ImageService
{
	private final ImageRepository imageRepository;
	private final StorageService storageService;
	private final Tika tika;

	public ImageService(ImageRepository imageRepository, StorageService storageService)
	{
		this.imageRepository = imageRepository;
		this.storageService = storageService;
		this.tika = new Tika();
	}

	public boolean isSupportedContentType(@NotNull MultipartFile imageFile)
	{
		try
		{
			String mimeType = this.tika.detect(imageFile.getInputStream());
			return switch (mimeType)
			{
				case "image/jpeg", "image/png", "image/webp", "image/avif" -> true;
				default -> false;
			};
		} catch (IOException e)
		{
			return false;
		}
	}

	@Transactional
	public Image addImageToPost(Post post, MultipartFile imageFile)
	{
		try
		{
			Image newImage = new Image();
			newImage = this.imageRepository.save(newImage);
			Resource savedResource = this.storageService.save(imageFile);
			newImage.setUrl(savedResource.getURL()
			                             .toString());
			return this.imageRepository.save(newImage);
		} catch (IOException e)
		{
			throw new RuntimeException(e);
		}
	}

	public Optional<Image> getImage(UUID id)
	{
		return this.imageRepository.findById(id);
	}

	public void deleteImageById(UUID id)
	{
		this.imageRepository.deleteById(id);
	}
}
