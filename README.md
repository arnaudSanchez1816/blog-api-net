# Blog-API

[![Blog-api CI](https://github.com/arnaudSanchez1816/blog-api-net/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/arnaudSanchez1816/blog-api-net/actions/workflows/ci.yml)

A blogging platform built with Spring and React.

This is a reimplementation of the back-end API of my already existing [Node.js/Express version](https://github.com/arnaudSanchez1816/blog-api).

## Features

- A RESTful API to easily interact with your blog's data.
- A custom content management system to create, edit and publish posts and moderate comments.
- JWT-based authentication and role based authorization to ensure only authorized users can manage your blog.
- Intuitive user interface for browsing, reading and filtering posts.

## Screenshot of the React client

<img width="1301" height="897" alt="client" src="https://github.com/user-attachments/assets/61548fef-0b2d-4d58-a28a-27c5d7c9fe66" />

## Installation

```sh
# Clone the repo
git clone https://github.com/arnaudSanchez1816/blog-api-spring.git
cd blog-api-spring

# Install dependencies
pnpm install

# Run backend api
cd services/api-spring
./mvnw spring-boot:run

# Create client .env files
cd -
cp ./apps/web/.env.example ./apps/web/.env
cp ./apps/cms/.env.example ./apps/cms/.env

# Run client apps
pnpm exec turbo dev
```

## 📖 OpenAPI Documentation

Swagger UI is available at: [http://localhost:3000/api/v1/swagger-ui.html](http://localhost:3000/api/v1/swagger-ui.html)

API documentation (OAS 3.1) is accessible at: [http://localhost:3000/api/v1/api-docs](http://localhost:3000/api/v1/api-docs)

## Apps and Packages

This monorepo includes the following packages/apps:

- `web`: a [React](https://react.dev/) app for the client frontend
- `cms`: a React content management system to manage the blog
- `api-spring`: a RESTful API built with [Spring](https://spring.io/)
- `@repo/ui`: a React component/hooks library shared by both `web` and `cms` applications
- `@repo/auth-provider`: a custom hook used to handle authentication via JWT token
- `@repo/client-api`: a library facade to the RESTful API used by both `web` and `cms` applications
- `@repo/zod-schemas`: [Zod](https://zod.dev/) schemas representing the model of the blog
- `@repo/eslint-config`: `eslint` configurations
- `@repo/tailwind-config`: `tailwindcss` configuration
- `@repo/heroui-config`: [heroui](https://www.heroui.com/) UI library configuration

## 📌 Endpoints

| Endpoint            | Method | Description                                      |
| ------------------- | ------ | ------------------------------------------------ |
| /auth/login         | POST   | Login a user                                     |
| /auth/logout        | GET    | Logout a user, clears http-only JWT token cookie |
| /auth/token         | GET    | Generate a new JWT access token                  |
| /users              | POST   | Create a new user                                |
| /users/me           | GET    | Get current user details                         |
| /users/me/posts     | GET    | Get current user posts                           |
| /posts/             | GET    | Get all published posts                          |
| /posts/             | POST   | Create a new post                                |
| /posts/:id          | GET    | Retrieve an existing post by ID                  |
| /posts/:id          | PUT    | Update an existing post by ID                    |
| /posts/:id          | DELETE | Delete an existing post by ID                    |
| /posts/:id/comments | GET    | Get comments of an existing post by ID           |
| /posts/:id/comments | POST   | Create a new comment for an existing post by ID  |
| /posts/:id/publish  | POST   | Publish an existing post by ID                   |
| /posts/:id/hide     | POST   | Unpublished an existing post by ID               |
| /comments/:id       | GET    | Retrieve an existing comment by ID               |
| /comments/:id       | PUT    | Update an existing comment by ID                 |
| /comments/:id       | DELETE | Delete an existing comment by ID                 |
| /tags/              | GET    | Get all existing tags                            |
| /tags/              | POST   | Create a new tag                                 |
| /tags/:id           | GET    | Get an existing tag by ID or slug                |
| /tags/:id           | PUT    | Update an existing tag by ID or slug             |
| /tags/:id           | DELETE | Delete an existing tag by ID or slug             |
