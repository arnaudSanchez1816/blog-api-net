package com.blog.api.apispring.service;

import com.blog.api.apispring.config.AwsS3BucketProperties;
import io.awspring.cloud.s3.S3Template;
import jakarta.validation.constraints.NotNull;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

import java.io.IOException;

@Service
@EnableConfigurationProperties(value = {AwsS3BucketProperties.class})
class StorageService
{

	private final AwsS3BucketProperties bucketProperties;
	private final S3Template s3Template;

	public StorageService(AwsS3BucketProperties bucketProperties, S3Template s3Template)
	{
		this.bucketProperties = bucketProperties;
		this.s3Template = s3Template;
	}

	public void save(@NotNull MultipartFile file) throws IOException
	{
		String originalFilename = file.getOriginalFilename();
		String bucketName = bucketProperties.getBucketName();
		s3Template.upload(bucketName, originalFilename, file.getInputStream());
	}

	public void delete(@NotNull String objectKey)
	{
		String bucketName = bucketProperties.getBucketName();
		s3Template.deleteObject(bucketName, objectKey);
	}
}
