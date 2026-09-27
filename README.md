# WhatFYN — What Fields You Need?

> Json with four hundred fields? What fields do you need?

An ASP.NET Core Web API output formatter that lets clients ask for **only the JSON fields they need**. It doesn't change your controllers: the client lists the fields it wants in a request header or the query string, and the response comes back with only those fields.

## How it works

WhatFYN adds an output formatter that sits right before your app's normal JSON formatter. When a request lists fields in the `x-only-fields` header or the `fields` query parameter, the formatter:

1. serializes the action result with System.Text.Json, using your app's MVC JSON options (camelCase by default),
2. removes every top-level property that isn't in the list,
3. writes the trimmed JSON to the response.

Requests that don't list any fields (or send an empty list) go to your normal JSON formatter, untouched. If a request has both, the header wins. String and stream results keep their own formatters.

## Usage

Requires .NET 8 or later.

The package isn't on NuGet yet. To use it now, reference the project [`src/WhatFYN`](src/WhatFYN) or build the package yourself with `dotnet pack src/WhatFYN -c Release`.

Register it with MVC:

```csharp
builder.Services.AddControllers().AddWhatFYN();
```

The header and query parameter names can be changed:

```csharp
builder.Services.AddControllers().AddWhatFYN(o =>
{
    o.HeaderName = "x-fields";
    o.QueryParameterName = "select";
});
```

### Example

Given an action that returns:

```json
{ "id": 42, "name": "Ada", "email": "ada@example.com", "address": { "city": "London" } }
```

Request only `id` and `name`:

```http
GET /api/customers/42
x-only-fields: id;name
```

or, easier to try in a browser and friendlier to HTTP caches, since the URL alone identifies the response:

```http
GET /api/customers/42?fields=id;name
```

Response:

```json
{ "id": 42, "name": "Ada" }
```

## What it does (and doesn't) save

WhatFYN trims the **response payload**: less data over the wire and less for the client to parse. It does **not** make the server do less work. The action still loads and builds the full object, all four hundred fields, before the formatter throws most of them away.

If the real cost is in the query itself, filter at the data layer instead:

- a dynamic `Select` projection in Entity Framework,
- [OData](https://learn.microsoft.com/odata/) `$select`,
- [GraphQL](https://graphql.org/).

WhatFYN is meant as the lightweight option. It has no extra dependencies, works with the controllers you already have, and fits when bandwidth or client-side parsing is the bottleneck.

## Limitations

- Field names are separated by semicolons, match case-sensitively, and must be in the serialized casing (camelCase by default).
- Only **top-level** properties are filtered. Nested paths such as `address.city` aren't supported.
- Only object results are filtered. Collections and single values are returned unchanged.
- Serialization always uses System.Text.Json. An app that switched MVC to Newtonsoft.Json still gets System.Text.Json output for filtered responses, so Newtonsoft attributes such as `[JsonProperty]` are ignored there.

## Development

```sh
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~FieldFilterTests.Returns_only_the_requested_fields"
```
