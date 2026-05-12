# Foundatio.Skeleton

A modern .NET 10 skeleton application built with [Foundatio](https://github.com/FoundatioFx/Foundatio), [Foundatio.Mediator](https://github.com/FoundatioFx/Foundatio.Mediator), ASP.NET Core minimal APIs, Aspire, and OpenTelemetry.

[![Build](https://github.com/FoundatioFx/Foundatio.Skeleton/actions/workflows/build.yaml/badge.svg)](https://github.com/FoundatioFx/Foundatio.Skeleton/actions/workflows/build.yaml)

## Features

- **ASP.NET Core Minimal APIs** — Auto-generated endpoints via Foundatio.Mediator
- **Foundatio.Mediator** — Convention-based mediator with source generators, zero reflection
- **Foundatio 13** — Caching, queuing, messaging, file storage, locking, jobs
- **Aspire AppHost** — Local dev orchestration with Redis and Mailpit
- **OpenTelemetry** — Tracing, metrics, and Prometheus endpoint
- **xUnit v3** — Modern testing with code coverage
- **GitHub Codespaces** — Ready-to-code devcontainer
- **GitHub Actions CI** — Build, test, coverage reporting

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Docker](https://www.docker.com/get-docker) (for Aspire AppHost)

### Run with Aspire (recommended)

```bash
aspire run
```

This starts Redis, Mailpit, and the Web API with the Aspire dashboard for traces/metrics/logs.

### Run standalone

```bash
dotnet run --project src/Foundatio.Skeleton.Web
```

### Run tests

```bash
dotnet test
```

### API Documentation

When running, visit `/docs` for the Scalar API reference.

Use the `.http` files in `tests/http/` with the VS Code [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension.

## Project Structure

```text
src/
  Foundatio.Skeleton.AppHost/     # Aspire orchestrator (Redis, Mailpit)
  Foundatio.Skeleton.Core/        # Domain models, services, Foundatio bootstrapping
  Foundatio.Skeleton.Insulation/  # Infrastructure overrides (Redis, MailKit)
  Foundatio.Skeleton.Web/         # ASP.NET Core minimal API + OTel
tests/
  Foundatio.Skeleton.Tests/       # xUnit v3 integration tests
  http/                           # .http request files
```

## Architecture

- **Core** registers in-memory defaults for all Foundatio abstractions (cache, message bus, queues, storage, locks) plus health checks
- **Insulation** conditionally replaces with Redis/MailKit when configured
- **Web** wires up the HTTP pipeline, OpenTelemetry, Foundatio.Mediator, and auto-generated API endpoints
- **Foundatio.Mediator** serves as the mediator — handlers are discovered by convention at compile time, endpoints are auto-generated from message types

## GitHub Codespaces

Click "Code → Codespaces → New codespace" to get a fully configured dev environment with .NET, Redis, and Mailpit.

## License

Apache 2.0
