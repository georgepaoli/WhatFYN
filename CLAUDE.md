# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Git in this repo

- This repo overrides the global rule about waiting to be asked before committing: here Claude may stage and commit its own work without asking first. Commits keep the user's configured git identity as the author. Credit Claude as co-author by ending the message with the attribution line (`Co-Authored-By: Claude ...`).
- The permission covers committing only. Pushing, creating branches and opening PRs still need an explicit request.
- The folder is owned by the Administrators group, so git refuses to run ("dubious ownership"). Pass `-c safe.directory=C:/george/git/github/me/WhatFYN` on each command rather than changing the global git config.

## What this is

WhatFYN ("What Fields You Need") is an ASP.NET Core output formatter that trims JSON responses to the fields the client asks for. The README covers usage and current limitations. Keep its Limitations section in sync when behavior changes.

## Commands

```sh
dotnet build
dotnet test                                   # runs on net8.0 and net10.0
dotnet test --filter "FullyQualifiedName~FieldFilterTests.Returns_only_the_requested_fields"
dotnet test -f net10.0                        # single target framework
dotnet pack src/WhatFYN -c Release
```

The repo's `nuget.config` clears the package sources and keeps only nuget.org. The user's machine-level NuGet config includes a private feed that returns 401, and without the override restore fails.

## Architecture

- `src/WhatFYN` targets net8.0 and net10.0. It has no package dependencies, only the `Microsoft.AspNetCore.App` framework reference.
- `AddWhatFYN()` (on `IMvcBuilder`) registers an `IConfigureOptions<MvcOptions>`. That setup builds `WhatFYNOutputFormatter` with the app's MVC `JsonOptions` and inserts it **right before the first output formatter that supports `application/json`**, so string and stream results keep their own formatters. The extension lives in the `Microsoft.Extensions.DependencyInjection` namespace, following the ASP.NET convention.
- The formatter handles normal JSON media types. It opts in per request through `CanWriteResult`, which returns `false` unless the request asks for fields. When it returns `false`, MVC falls through to the app's regular JSON formatter, so requests without fields are untouched.
- Filtering serializes the result to a `JsonNode` with System.Text.Json, prunes it, and writes it back out. `FieldSelection` parses the field list into a tree of dotted paths (a `null` child means "the whole value") and does the pruning recursively, descending into arrays item by item.
- Tests (`tests/WhatFYN.Tests`) spin up an in-memory app with `TestServer` and real controllers from the test assembly (`TestApp.cs`). Output formatters only run for MVC controllers, not minimal API endpoints, so new test scenarios need a controller action.
