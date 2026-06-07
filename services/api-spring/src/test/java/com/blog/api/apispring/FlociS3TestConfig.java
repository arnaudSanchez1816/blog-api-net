package com.blog.api.apispring;

import io.floci.testcontainers.FlociContainer;
import jakarta.annotation.PostConstruct;
import org.slf4j.event.Level;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.context.annotation.Bean;
import org.springframework.test.context.DynamicPropertyRegistrar;
import org.testcontainers.junit.jupiter.Container;
import org.testcontainers.junit.jupiter.Testcontainers;
import software.amazon.awssdk.auth.credentials.AwsBasicCredentials;
import software.amazon.awssdk.auth.credentials.StaticCredentialsProvider;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.s3.S3Client;

import java.net.URI;

@Testcontainers(disabledWithoutDocker = true)
@TestConfiguration
public class FlociS3TestConfig
{
	@Container
	private final static FlociContainer floci;

	static
	{
		floci = new FlociContainer().withLogLevel(Level.DEBUG);
		floci.start();
	}

	// Bucket name is sourced from application.properties (blog-api.aws.s3.bucket-name).
	@Value("${blog-api.aws.s3.bucket-name}")
	private String bucketName;

	@PostConstruct
	void createBucket()
	{
		try (S3Client s3Client = S3Client.builder()
		                                 .endpointOverride(URI.create(floci.getEndpoint()))
		                                 .region(Region.of(floci.getRegion()))
		                                 .credentialsProvider(StaticCredentialsProvider.create(AwsBasicCredentials.create(
												 floci.getAccessKey(),
												 floci.getSecretKey())))
		                                 .forcePathStyle(true)
		                                 .build())
		{
			s3Client.createBucket(builder -> builder.bucket(bucketName));
		}
	}

	@Bean
	DynamicPropertyRegistrar flociS3Properties()
	{
		return registry -> {
			// spring cloud aws properties supplied by the Floci container
			registry.add("spring.cloud.aws.credentials.access-key", floci::getAccessKey);
			registry.add("spring.cloud.aws.credentials.secret-key", floci::getSecretKey);
			registry.add("spring.cloud.aws.s3.region", floci::getRegion);
			registry.add("spring.cloud.aws.s3.endpoint", floci::getEndpoint);
		};
	}
}
