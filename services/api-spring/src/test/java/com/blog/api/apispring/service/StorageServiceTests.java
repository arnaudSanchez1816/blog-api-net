package com.blog.api.apispring.service;

import com.blog.api.apispring.FlociS3TestConfig;
import io.awspring.cloud.s3.S3Template;
import net.bytebuddy.utility.RandomString;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.context.annotation.Import;
import org.springframework.mock.web.MockMultipartFile;
import org.springframework.test.context.ActiveProfiles;
import org.springframework.web.multipart.MultipartFile;

import java.io.ByteArrayInputStream;
import java.io.IOException;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
@ActiveProfiles("test")
@Import(FlociS3TestConfig.class)
public class StorageServiceTests
{
	@Autowired
	private S3Template s3Template;
	@Autowired
	private StorageService storageService;
	@Value("${blog-api.aws.s3.bucket-name}")
	private String bucketName;

	@Test
	void shouldSaveFileSuccessfullyToBucket()
	{
		// Prepare test file to upload
		String key = RandomString.make(10) + ".txt";
		String fileContent = RandomString.make(50);
		MultipartFile fileToUpload = createTextFile(key, fileContent);

		// Invoke method under test
		try
		{
			storageService.save(fileToUpload);
			// Verify that the file is saved successfully in S3 bucket
			boolean isFileSaved = s3Template.objectExists(bucketName, key);
			assertThat(isFileSaved).isTrue();
		} catch (IOException e)
		{
			throw new RuntimeException(e);
		}
	}

	private MultipartFile createTextFile(String fileName, String content)
	{
		try
		{
			byte[] fileContentBytes = content.getBytes();
			ByteArrayInputStream inputStream = new ByteArrayInputStream(fileContentBytes);
			return new MockMultipartFile(fileName, fileName, "text/plain", inputStream);
		} catch (IOException e)
		{
			throw new RuntimeException(e);
		}
	}
}
