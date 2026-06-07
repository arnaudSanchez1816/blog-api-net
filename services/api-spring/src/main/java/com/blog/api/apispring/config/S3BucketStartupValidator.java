package com.blog.api.apispring.config;

import io.awspring.cloud.s3.S3Template;
import jakarta.annotation.PostConstruct;
import org.springframework.stereotype.Component;

/**
 * Verifies at startup that the configured S3 bucket exists, failing fast (before the
 * web server starts accepting traffic) if it does not.
 */
@Component
public class S3BucketStartupValidator
{

	private final AwsS3BucketProperties properties;
	private final S3Template s3Template;

	public S3BucketStartupValidator(AwsS3BucketProperties properties, S3Template s3Template)
	{
		this.properties = properties;
		this.s3Template = s3Template;
	}

	@PostConstruct
	void verifyBucketExists()
	{
		if (!properties.isVerifyBucketOnStartup())
		{
			return;
		}

		String bucketName = properties.getBucketName();
		if (!s3Template.bucketExists(bucketName))
		{
			throw new IllegalStateException("Configured AWS S3 bucket '" + bucketName + "' does not exist. " +
			                                "Set 'blog-api.aws.s3.bucket-name' to an existing bucket.");
		}
	}
}
