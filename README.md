# Blog-API

[![Blog-api CI](https://github.com/arnaudSanchez1816/blog-api-net/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/arnaudSanchez1816/blog-api-net/actions/workflows/ci.yml)

A blogging platform built with ASP.NET Core and React.

This is a reimplementation of the back-end API of my already existing [Node.js/Express](https://github.com/arnaudSanchez1816/blog-api) and [Java/Springboot](https://github.com/arnaudSanchez1816/blog-api-spring) versions.

🔗 **Live demo:** [blogapinet.asanchez1816.cloud](https://blogapinet.asanchez1816.cloud)

## Features

- A RESTful API to easily interact with your blog's data.
- A custom content management system to create, edit and publish posts and moderate comments.
- JWT-based authentication and role based authorization to ensure only authorized users can manage your blog.
- Intuitive user interface for browsing, reading and filtering posts.

## Tech Stack

### Backend

- ASP.NET Core 10 (C#) with an installer pattern for cross-cutting setup
- Entity Framework Core + PostgreSQL, migrations applied automatically on startup
- JWT access tokens with rotating refresh tokens (HttpOnly cookie, custom auth scheme)
- Permission-based authorization, including resource-ownership rules (e.g. authors can edit their own posts)
- FluentValidation with auto-validation on model binding
- ProblemDetails-based exception handling
- OpenAPI generation, versioned endpoints (`Asp.Versioning`), docs served via Scalar
- xUnit v3 unit tests + integration tests against a real PostgreSQL instance via Testcontainers and Respawn

### Frontend

- React + TypeScript, built with Vite
- Tailwind CSS and [HeroUI](https://www.heroui.com/)
- Zod schemas shared between apps as the API contract
- Turborepo + pnpm workspaces monorepo

### Infrastructure

- Dockerized, self-hosted on a VPS via [Dokploy](https://dokploy.com/)
- CI/CD with GitHub Actions: build/test on push, then trigger a Dokploy deployment only if tests pass

## Screenshots

### React client

<img width="1301" height="897" alt="client" src="https://github.com/user-attachments/assets/61548fef-0b2d-4d58-a28a-27c5d7c9fe66" />

### Content management system (post editor)

<!-- TODO: add CMS post-edit screenshot -->

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) and [pnpm](https://pnpm.io/)
- PostgreSQL, reachable locally (or Docker to run one — see `services/api/Blog-Api/compose-image.yml` for a ready-made Postgres + API compose setup)
- Docker (only required to run the backend's integration test suite, which spins up Postgres via Testcontainers)

## Installation

```sh
# Clone the repo
git clone https://github.com/arnaudSanchez1816/blog-api-net.git
cd blog-api-net
```

### API

#### Locally (Requires PostgresSQL instance and .NET 10 SDK)

```sh
cd services/api/Blog-Api
dotnet restore
dotnet run --project Blog-Api
```

#### Using Docker image

```sh
cd services/api/Blog-Api
docker compose -f compose-image.yml up --build
```

### Build frontend

```sh
# Install dependencies
cd -
pnpm install

# Create client .env files
# If using the api docker image, set VITE_API_URL to http://localhost:8080/api/v1/
cp ./apps/web/.env.example ./apps/web/.env
cp ./apps/cms/.env.example ./apps/cms/.env

# Run client apps
pnpm exec turbo dev
```

## 📖 OpenAPI Documentation

Scalar is available at: [https://api.blogapinet.asanchez1816.cloud/scalar/](https://api.blogapinet.asanchez1816.cloud/scalar/)

OpenAPI document is accessible at: [https://api.blogapinet.asanchez1816.cloud/openapi/v1.json](https://api.blogapinet.asanchez1816.cloud/openapi/v1.json)

## Apps and Packages

This monorepo includes the following packages/apps:

- `web`: a [React](https://react.dev/) app for the client frontend
- `cms`: a React content management system to manage the blog
- `api`: a RESTful API built with [ASP.NET Core](https://dotnet.microsoft.com/en-us/apps/aspnet)
- `@repo/ui`: a React component/hooks library shared by both `web` and `cms` applications
- `@repo/auth-provider`: a custom hook used to handle authentication via JWT token
- `@repo/client-api`: a library facade to the RESTful API used by both `web` and `cms` applications
- `@repo/zod-schemas`: [Zod](https://zod.dev/) schemas representing the model of the blog
- `@repo/eslint-config`: `eslint` configurations
- `@repo/tailwind-config`: `tailwindcss` configuration
- `@repo/heroui-config`: [heroui](https://www.heroui.com/) UI library configuration

## 📌 Endpoints

| Endpoint                     | Method | Description                                          |
| ---------------------------- | ------ | ---------------------------------------------------- |
| /api/v1/auth/login           | POST   | Login a user                                         |
| /api/v1/auth/logout          | GET    | Logout a user, clears http-only refresh token cookie |
| /api/v1/auth/token           | GET    | Generate a new JWT access token                      |
| /api/v1/users/me             | GET    | Get current user details                             |
| /api/v1/users/me/posts       | GET    | Get current user posts                               |
| /api/v1/posts/               | GET    | Get all published posts                              |
| /api/v1/posts/               | POST   | Create a new post                                    |
| /api/v1/posts/:slug          | GET    | Retrieve an existing post by slug                    |
| /api/v1/posts/:slug          | PUT    | Update an existing post by slug                      |
| /api/v1/posts/:slug          | DELETE | Delete an existing post by slug                      |
| /api/v1/posts/:slug/comments | GET    | Get comments of an existing post by slug             |
| /api/v1/posts/:slug/comments | POST   | Create a new comment for an existing post by slug    |
| /api/v1/comments/:id         | GET    | Retrieve an existing comment by ID                   |
| /api/v1/comments/:id         | PUT    | Update an existing comment by ID                     |
| /api/v1/comments/:id         | DELETE | Delete an existing comment by ID                     |
| /api/v1/tags/                | GET    | Get all existing tags                                |
| /api/v1/tags/                | POST   | Create a new tag                                     |
| /api/v1/tags/:slug           | GET    | Get an existing tag by slug                          |
| /api/v1/tags/:slug           | PUT    | Update an existing tag by slug                       |
| /api/v1/tags/:slug           | DELETE | Delete an existing tag by slug                       |
| /api/v1/tags/id/:id          | GET    | Get an existing tag by id                            |

## License

[MIT](./LICENSE)
