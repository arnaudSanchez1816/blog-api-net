package com.blog.api.apispring.config;

import jakarta.validation.constraints.NotBlank;
import lombok.Getter;
import lombok.Setter;
import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.validation.annotation.Validated;

@Getter
@Setter
@Validated
@ConfigurationProperties(prefix = "blog-api.aws.s3")
public class AwsS3BucketProperties
{

	@NotBlank(message = "S3 bucket name must be configured")
	private String bucketName;

	/**
	 * Whether to verify at startup that the configured bucket exists.
	 * Enabled by default so the application fails fast on a misconfigured bucket;
	 * can be disabled (e.g. in tests that do not provide an S3 bucket).
	 */
	private boolean verifyBucketOnStartup = true;
}
