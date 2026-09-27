# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Git in this repo

- This repo overrides the global rule about waiting to be asked before committing: here Claude may stage and commit its own work without asking first. Commits keep the user's configured git identity as the author. Credit Claude as co-author by ending the message with the attribution line (`Co-Authored-By: Claude ...`).
- The permission covers committing only. Pushing, creating branches and opening PRs still need an explicit request.
- The folder is owned by the Administrators group, so git refuses to run ("dubious ownership"). Pass `-c safe.directory=C:/george/git/github/me/WhatFYN` on each command rather than changing the global git config.

## What this is

WhatFYN ("What Fields You Need") is an ASP.NET Core Web API custom output formatter that lets clients request only the JSON fields they need from a response.

The repo holds just one source file, [src/CustonOutputFormatter.cs](src/CustonOutputFormatter.cs). The file name misspells "Custom"; the class inside is `CustomOutputFormatter`. The repo has no `.csproj`, solution, tests, or sample host. It cannot be built on its own. To compile or try it, add the file to an ASP.NET Core project that references `Newtonsoft.Json` and register it:

```csharp
services.AddMvc(o => o.OutputFormatters.Insert(0, new WhatFYN.CustomOutputFormatter()));
```

## How the formatter works

- It derives from `TextOutputFormatter` and only handles the vendor media type `application/x-wfyn+json`. Content negotiation selects it only when the client sends `Accept: application/x-wfyn+json`. `CanWriteType` returns `true` for any type.
- The client must also send the `x-only-fields` header, a **semicolon-separated** list of field names (e.g. `id;name`). If the header is missing, the formatter throws `InvalidOperationException`.
- It serializes the response object with Newtonsoft.Json, using camelCase property names and ignoring nulls by default (overridable through the constructors). It parses the result into a `JObject` and removes every top-level property not named in the header.

Known limitations to keep in mind when changing it:
- Only **top-level** properties are filtered. It has no nested-path support.
- `JObject.Parse` throws if the action returns a collection or primitive, because the root must be a JSON object.
- Field names match exactly and case-sensitively against the camelCased output, so clients must send camelCase names.
