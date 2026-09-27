# WhatFYN — What Fields You Need?

> Json with four hundred fields? What fields do you need?

An ASP.NET Core Web API custom output formatter that lets clients ask for **only the JSON fields they need**. It doesn't change your controllers: the client sends two headers, and the response comes back with only the fields it asked for.

## How it works

The formatter registers a vendor media type, `application/x-wfyn+json`. When a request asks for that type through `Accept` and lists the wanted fields in `x-only-fields`, the formatter:

1. serializes the action result with Newtonsoft.Json (camelCase, nulls ignored),
2. removes every top-level property that isn't in the list,
3. writes the trimmed JSON to the response.

Requests that don't ask for this media type keep going to your normal JSON formatter.

## Usage

The project is a single file: [`src/CustonOutputFormatter.cs`](src/CustonOutputFormatter.cs). Copy it into your ASP.NET Core project, reference `Newtonsoft.Json`, and register the formatter:

```csharp
services.AddMvc(options =>
{
    options.OutputFormatters.Insert(0, new WhatFYN.CustomOutputFormatter());
});
```

The constructors also accept your own `JsonSerializerSettings` and/or `Encoding`.

### Example

Given an action that returns:

```json
{ "id": 42, "name": "Ada", "email": "ada@example.com", "address": { "city": "London" } }
```

Request only `id` and `name`:

```http
GET /api/customers/42
Accept: application/x-wfyn+json
x-only-fields: id;name
```

Response:

```json
{ "id": 42, "name": "Ada" }
```

## Limitations

- The `x-only-fields` header is **required** with this media type. Without it, the formatter throws `InvalidOperationException`.
- Field names are separated by semicolons, match case-sensitively, and must be in **camelCase**, the same as the serialized output.
- Only **top-level** properties are filtered. Nested paths such as `address.city` aren't supported.
- The action result must serialize to a JSON **object**. A collection or a single value (string, number, etc.) makes it throw.
