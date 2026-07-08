# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

This is a from-scratch .NET rewrite of the blog API backend for the `blog-api-net` monorepo. The previous
backend implementations (Express/Node.js, then Spring) have both been removed (see git history: "Remove
spring implementation", "Add dot net project"). Right now this project is just past the default ASP.NET Core
template stage — OpenAPI/Scalar docs are wired up, but there is no persistence layer, authentication, or
domain code (controllers, entities, services) yet. `WeatherForecastController` and `WeatherForecast.cs` are
leftover template scaffolding, not real functionality.

Expect to build out the domain model (posts, comments, tags, users) and JWT-based auth largely from scratch.
The sibling frontend apps (`apps/web`, `apps/cms`) and shared packages in the monorepo root already assume an
API shape — check `../../../packages/zod-schemas` and `../../../packages/client-api` in the monorepo for the
contract the frontend expects before designing new endpoints.

## Commands

Run all commands from this directory (`services/api/Blog-Api`, containing `Blog-Api.sln`).

```sh
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run (dev profile, serves on http://localhost:5276, opens Scalar UI at /scalar)
dotnet run --project Blog-Api

# Run with hot reload
dotnet watch --project Blog-Api run
```

There are no test projects in the solution yet — `dotnet test` has nothing to run until one is added.

## Architecture

- **Target framework**: net10.0, nullable reference types and implicit usings enabled, root namespace `BlogApi`.
- **Entry point**: `Blog-Api/Program.cs` — minimal hosting model. Service registration and middleware wiring
  happen through extension methods on `IServiceCollection`/`WebApplication`, not inline in `Program.cs`.
- **Installers pattern**: cross-cutting setup lives in `Blog-Api/Installers/*Installer.cs` as static classes
  exposing `Install*` extension methods (e.g. `OpenApiInstaller.InstallOpenApi` /
  `OpenApiInstaller.InstallScalar`). When adding new infrastructure (DB context, auth, etc.), follow this same
  pattern rather than inlining setup in `Program.cs`.
- **Options pattern**: configuration sections bind to POCOs in `Blog-Api/Options/*Options.cs` (e.g.
  `OpenApiOptions`), bound via `IConfiguration.Bind(nameof(TOptions), ...)` and registered as singletons inside
  the corresponding installer. Follow this convention for new configurable features rather than reading
  `IConfiguration` directly in application code.
- **API docs**: OpenAPI generation is native ASP.NET Core (`AddOpenApi`/`MapOpenApi`), served through
  `Scalar.AspNetCore` at `/scalar` (configurable via `OpenApiOptions`). Document/operation transformers in
  `OpenApiInstaller` add a Bearer JWT security scheme to any endpoint carrying `[Authorize]`, and camel-case
  query parameter names — keep this in mind when adding authenticated endpoints or new parameters.
- **Planned stack** (per package references already in `Blog-Api.csproj`, not yet wired up):
  `Microsoft.AspNetCore.Identity.EntityFrameworkCore` and `Npgsql.EntityFrameworkCore.PostgreSQL` — the
  intended persistence is EF Core against PostgreSQL with ASP.NET Core Identity for users/auth.
- **Monorepo context**: this directory is one package inside a pnpm/turborepo monorepo rooted at
  `blog-api-net/` (git top-level). The JS/TS tooling (`package.json`, `turbo.json`, pnpm workspaces) at the
  monorepo root does not apply to this .NET project directly, but the frontend apps and shared schema packages
  there define the API contract this service needs to satisfy.
