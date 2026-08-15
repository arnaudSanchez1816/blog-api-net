# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

This is a from-scratch .NET rewrite of the blog API backend for the `blog-api-net` monorepo (the previous
Express/Node.js and Spring implementations have both been removed). It is now feature-complete for its core
domain: JWT authentication with refresh-token rotation, role/permission-based authorization, posts, comments,
tags, EF Core migrations against PostgreSQL, health checks, and unit/integration test suites. New work is
additive (new endpoints, richer domain behavior) rather than foundational.

## Commands

Run all commands from this directory (`services/api/Blog-Api`, containing `Blog-Api.sln`), unless noted.

```sh
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run (dev profile "http", serves on http://localhost:3000, opens Scalar UI at /scalar)
dotnet run --project Blog-Api

# Run with hot reload
dotnet watch --project Blog-Api run

# Run all tests in a project
dotnet test --project Blog-Api.Unit
dotnet test --project Blog-Api.Integration

# Run a single test (dotnet test's MTP wrapper does not forward xUnit's -method/-class
# filters reliably; invoke the built test executable directly instead)
dotnet run --project Blog-Api.Unit -- -method "*MethodName*"
dotnet run --project Blog-Api.Unit -- -class "*ClassName*"

# Add a new EF Core migration (from Blog-Api/)
dotnet ef migrations add <Name>
```

- The test runner is `Microsoft.Testing.Platform` (set in `global.json`), using xUnit v3, `AwesomeAssertions`
  for fluent assertions, and `Moq` for mocking.
- `Blog-Api.Integration` uses `Testcontainers.PostgreSql` to spin up a real Postgres per test run and
  `Respawn` to reset state between tests — **Docker must be running** for integration tests to pass.
- `Blog-Api.Unit` covers services, authorization handlers, validators, and utilities in isolation (mocked
  dependencies, no database).

## Architecture

- **Solution layout**: `Blog-Api` (the web app), `Blog-Api.Contracts` (DTOs/request/response records shared
  by the API and referenced by both test projects — no logic), `Blog-Api.Unit`, `Blog-Api.Integration`.
- **Target framework**: net10.0, nullable reference types and implicit usings enabled, root namespace
  `BlogApi`.
- **Entry point**: `Blog-Api/Program.cs` — minimal hosting model. Service registration and middleware wiring
  happen through extension methods on `IServiceCollection`/`WebApplication`, not inline in `Program.cs`.
- **Installers pattern**: cross-cutting setup lives in `Blog-Api/Installers/*Installer.cs` as static classes
  exposing `Install*` extension methods, each typically providing both an `IServiceCollection` overload (DI
  registration) and a `WebApplication` overload (middleware wiring), called in that order from `Program.cs`.
  Follow this pattern for any new cross-cutting infrastructure.
- **Options pattern**: configuration sections bind to POCOs in `Blog-Api/Options/*Options.cs`, bound via
  `BindConfiguration`/`IConfiguration.Bind` and registered inside the corresponding installer. Follow this
  convention for new configurable features rather than reading `IConfiguration` directly in application code.

### Request pipeline / layering

Controllers (`Blog-Api/Controllers/V1`) → Services (`Blog-Api/Services/*`) → Repositories
(`Blog-Api/Repositories/*`) → `DataContext` (EF Core, `Blog-Api/Data/DataContext.cs`). Each service/repository
pair is interface-first (`IPostsService`/`PostsService`, `IPostsRepository`/`PostsRepository`, etc.) and
registered in `ServicesInstaller`/`RepositoriesInstaller`. `Blog-Api/Mapping/*MappingExtensions.cs` holds
domain-entity-to-contract mapping as extension methods (no AutoMapper).

- **Routes**: centralized as string constants in `Blog-Api/Routes/V1/ApiRoutes.cs` (e.g.
  `ApiRoutes.Posts.Base`), rather than inline literals on controller attributes. All routes are versioned via
  `api/v{version:apiVersion}/...` (`Asp.Versioning`, configured in `VersioningInstaller`, URL-segment
  versioned).
- **Validation**: FluentValidation validators live in `Blog-Api/Validation/Validators/**`, auto-invoked via
  `SharpGrip.FluentValidation.AutoValidation.Mvc` (no manual `ModelState` checks in controllers). Validators
  reference shared constants (e.g. `PostsValidationConstants`) rather than duplicating magic numbers.
- **Errors**: `ProblemDetails`-based, via `GlobalExceptionHandler` and `ExceptionHandlingInstaller`
  (`AddProblemDetails` + `UseExceptionHandler` + `UseStatusCodePages`), not ad-hoc try/catch in controllers.

### Domain model

`Blog-Api/Domain/*`: `BlogUser`/`BlogRole` (ASP.NET Core Identity, `Guid` keys), `Post` (has one `Author`
(`BlogUser`), many `Tags` — unidirectional many-to-many, many `Comments`), `Comment`, `Tag`, `RefreshToken`.
Entity configuration (indexes, max lengths, relationships) is centralized in `DataContext.OnModelCreating`
rather than via separate `IEntityTypeConfiguration<T>` classes — follow that convention for new entities.
`DataContext` trims unused ASP.NET Identity columns via `.Ignore(...)` (e.g. lockout/2FA fields are not used
by this project).

### Authentication & authorization

- **Access tokens**: short-lived JWTs (`Microsoft.AspNetCore.Authentication.JwtBearer`), issuer/audience/secret
  configured via `AppAuthenticationOptions` (`AppAuthenticationOptions__*` env vars in production).
- **Refresh tokens**: a custom authentication scheme (`RefreshTokenAuthenticationHandler`,
  `RefreshTokenAuthDefaults`) reads a rotating refresh token from an HttpOnly cookie
  (`__Http-REFRESHTOKEN`); rotation/expiry/cleanup logic lives in `Services/Tokens` (`TokensService`,
  `RefreshTokensCleanupService` — a background hosted service, interval configurable via
  `AppAuthenticationOptions.RefreshTokensCleanupInterval`).
- **Authorization is permission-based, not role-based, at the endpoint level**: controllers use
  `[HasPermission(Permissions.Posts.Update)]` (custom `AuthorizeAttribute` subclass in
  `Authorization/HasPermissionAttribute.cs`) rather than `[Authorize(Roles = ...)]`. Permission strings are
  defined in `Authorization/Permissions.cs`; roles (`Admin`, `Moderator`, `User`, and an implicit anonymous
  set) are mapped to permission lists in `Authorization/Roles.cs` — a user's effective permissions come from
  their role's permission list, checked via `PermissionAuthorizationHandler`/`PermissionPolicyProvider`
  against dynamically-generated policies (`Permissions.ToPermissionPolicy(...)`). When adding a new
  action, add a permission constant, decide which roles get it in `Roles.Permissions`, and guard the endpoint
  with `[HasPermission(...)]` — don't introduce new ad-hoc role checks.
- **Resource-level ownership**: e.g. `PostOwnerAuthorizationHandler` (an `AuthorizationHandler<TRequirement,
  Post>`) lets a post's author bypass a permission check on their own resource (e.g. update their own post
  without `Posts.Update`). Follow this pattern (a resource-typed handler on `PermissionRequirement`) for other
  "owner can act on their own resource" cases rather than special-casing it in the service/controller.

### API docs & health

- OpenAPI generation is native ASP.NET Core (`AddOpenApi`/`MapOpenApi`), served through `Scalar.AspNetCore` at
  `/scalar` (configurable via `OpenApiOptions`; note `/scalar` without a trailing slash 302-redirects to
  `/scalar/`). Document/operation transformers in `OpenApiInstaller` add a Bearer JWT + refresh-token-cookie
  security scheme to endpoints, camel-case query parameter names, and strip parameters that are always
  overridden server-side — keep this in mind when adding authenticated endpoints or new parameters.
- `GET /health` (`HealthChecksInstaller`) returns a custom JSON shape (`HealthCheckResponse`/`HealthCheck` in
  `Blog-Api.Contracts/Health`), currently checking only the Postgres `DataContext` via
  `AddDbContextCheck<DataContext>()` — add one `.Add*Check(...)` per new external dependency as they're
  introduced (there are none besides Postgres today). This route is an exact-path match with no trailing-slash
  redirect (unlike `/scalar`) — `/health/` 404s.

### Persistence & seeding

- EF Core against PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`, snake_case naming via
  `EFCore.NamingConventions`), configured in `DbInstaller`. Migrations live in `Blog-Api/Migrations` and are
  applied automatically on startup (`app.MigrateDatabase()` in `Program.cs`), not via a separate CLI step in
  deployment.
- Optional admin-user seeding on startup is controlled by `DatabaseSeedingOptions` (`DatabaseSeedingOptions__*`
  env vars) — see `Blog-Api/Seeding`.

### Deployment

- `Blog-Api/Dockerfile`: multi-stage build (`sdk` → publish → `aspnet` runtime), listens on container port
  8080 (`ASPNETCORE_HTTP_PORTS` default from the base image), runs as non-root `$APP_UID`, with a
  pre-created `/home/app/keys` directory for ASP.NET Core Data Protection key persistence (mounted as a
  volume in compose).
- `compose-image.yml`: local-only compose (includes its own Postgres service, plaintext dev secrets).
  `compose-dokploy.yml`: production compose for Dokploy — no bundled Postgres (points at an
  already-deployed Dokploy Postgres service instead), no custom Docker network (relies on Dokploy's
  per-project shared network), and all secrets/config passed as `${VAR}` substitutions resolved from
  Dokploy's environment-variable UI rather than hardcoded.
- CI/CD (`.github/workflows/ci.yml`): on push to `main` (path-filtered to `services/api/**`) or manual
  `workflow_dispatch`, builds and tests, then — only if that succeeds — a `deploy` job calls Dokploy's
  `POST /api/compose.deploy` REST endpoint (an authenticated API-key call, intentionally *not* Dokploy's
  webhook URL or its "Auto Deploy" GitHub integration, both of which are gated by the same `autoDeploy` flag
  and can't be triggered independently of raw git pushes). Dokploy's own "Auto Deploy" toggle for this
  compose is deliberately left **off** so pushes only deploy via this CI-gated path.

## Monorepo context

This directory is one package inside a pnpm/turborepo monorepo rooted at `blog-api-net/` (git top-level). The
JS/TS tooling (`package.json`, `turbo.json`, pnpm workspaces) at the monorepo root does not apply to this
.NET project directly, but the frontend apps (`apps/web`, `apps/cms`) and shared schema packages
(`packages/zod-schemas`, `packages/client-api`) there define the API contract this service needs to satisfy —
check those before designing new endpoints or changing response shapes. Note the monorepo root `README.md`
still describes the old Spring implementation (different port/Swagger paths) and is stale with respect to
this service; don't treat it as authoritative for this project.
