package com.blog.api.apispring.service;

import com.blog.api.apispring.repository.ImageRepository;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.mock.web.MockMultipartFile;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

@ExtendWith(MockitoExtension.class)
class ImageServiceTests
{
	// Magic-byte signatures Tika uses to detect the content type from the file contents.
	private static final byte[] PNG_SIGNATURE = {(byte) 0x89, 'P', 'N', 'G', 0x0D, 0x0A, 0x1A, 0x0A};
	private static final byte[] JPEG_SIGNATURE = {(byte) 0xFF, (byte) 0xD8, (byte) 0xFF, (byte) 0xE0, 0x00, 0x10, 'J',
			'F', 'I', 'F', 0x00
	};
	private static final byte[] WEBP_SIGNATURE = {'R', 'I', 'F', 'F', 0x1A, 0x00, 0x00, 0x00, 'W', 'E', 'B', 'P', 'V',
			'P', '8', ' '
	};
	private static final byte[] AVIF_SIGNATURE = {0x00, 0x00, 0x00, 0x00, 'f', 't', 'y', 'p', 'a', 'v', 'i', 'f'};
	private static final byte[] GIF_SIGNATURE = {'G', 'I', 'F', '8', '9', 'a'};

	@Mock
	private ImageRepository imageRepository;
	@Mock
	private StorageService storageService;

	private ImageService imageService;

	@BeforeEach
	void setUp()
	{
		imageService = new ImageService(imageRepository, storageService);
	}

	@Test
	void isSupportedContentType_ShouldReturnTrue_ForJpegFile()
	{
		MultipartFile jpegFile = new MockMultipartFile("image", "image.jpg", "image/jpeg", JPEG_SIGNATURE);

		assertThat(imageService.isSupportedContentType(jpegFile)).isTrue();
	}

	@Test
	void isSupportedContentType_ShouldReturnTrue_ForPngFile()
	{
		MultipartFile pngFile = new MockMultipartFile("image", "image.png", "image/png", PNG_SIGNATURE);

		assertThat(imageService.isSupportedContentType(pngFile)).isTrue();
	}

	@Test
	void isSupportedContentType_ShouldReturnTrue_ForWebpFile()
	{
		MultipartFile webpFile = new MockMultipartFile("image", "image.webp", "image/webp", WEBP_SIGNATURE);

		assertThat(imageService.isSupportedContentType(webpFile)).isTrue();
	}

	@Test
	void isSupportedContentType_ShouldReturnTrue_ForAvifFile()
	{
		MultipartFile avifFile = new MockMultipartFile("image", "image.avif", "image/avif", AVIF_SIGNATURE);

		assertThat(imageService.isSupportedContentType(avifFile)).isTrue();
	}

	@Test
	void isSupportedContentType_ShouldReturnFalse_ForUnsupportedImageType()
	{
		MultipartFile gifFile = new MockMultipartFile("image", "image.gif", "image/gif", GIF_SIGNATURE);

		assertThat(imageService.isSupportedContentType(gifFile)).isFalse();
	}

	@Test
	void isSupportedContentType_ShouldReturnFalse_ForNonImageFile()
	{
		MultipartFile textFile = new MockMultipartFile("file",
				"notes.txt",
				"text/plain",
				"just some plain text".getBytes());

		assertThat(imageService.isSupportedContentType(textFile)).isFalse();
	}

	@Test
	void isSupportedContentType_ShouldDetectContentType_RegardlessOfDeclaredContentType()
	{
		// A plain text file masquerading as a PNG via its declared content type should still be rejected,
		// because detection is based on the actual file contents.
		MultipartFile disguisedFile = new MockMultipartFile("image",
				"image.png",
				"image/png",
				"not really a png".getBytes());

		assertThat(imageService.isSupportedContentType(disguisedFile)).isFalse();
	}

	@Test
	void isSupportedContentType_ShouldReturnFalse_WhenInputStreamThrowsIOException() throws IOException
	{
		MultipartFile failingFile = mock(MultipartFile.class);
		when(failingFile.getInputStream()).thenThrow(new IOException("stream unavailable"));

		assertThat(imageService.isSupportedContentType(failingFile)).isFalse();
	}
}
