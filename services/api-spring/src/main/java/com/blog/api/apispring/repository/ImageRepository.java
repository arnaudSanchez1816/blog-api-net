package com.blog.api.apispring.repository;

import com.blog.api.apispring.model.Image;
import org.springframework.data.repository.CrudRepository;

import java.util.UUID;

public interface ImageRepository extends CrudRepository<Image, UUID>
{
}