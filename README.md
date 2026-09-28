# WhatFYN — What Fields You Need?

> Json with four hundred fields? What fields do you need?

An ASP.NET Core Web API output formatter that lets clients ask for **only the JSON fields they need**. It doesn't change your controllers: the client lists the fields it wants in a request header or the query string, and the response comes back with only those fields.

## How it works

WhatFYN adds an output formatter that sits right before your app's normal JSON formatter. When a request lists fields in the `x-only-fields` header or the `fields` query parameter, the formatter:

1. serializes the action result with System.Text.Json, using your app's MVC JSON options (camelCase by default),
2. removes every property that isn't in the list, at any depth,
3. writes the trimmed JSON to the response.

Requests that don't list any fields (or send an empty list) go to your normal JSON formatter, untouched. If a request has both, the header wins. String and stream results keep their own formatters.

## Usage

Requires .NET 8 or later.

Install the [NuGet package](https://www.nuget.org/packages/WhatFYN):

```sh
dotnet add package WhatFYN
```

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
x-only-fields: id,name
```

or, easier to try in a browser and friendlier to HTTP caches, since the URL alone identifies the response:

```http
GET /api/customers/42?fields=id,name
```

Response:

```json
{ "id": 42, "name": "Ada" }
```

Fields can be separated by commas or semicolons, and names are matched ignoring case, so `ID;Name` works too.

### Nested fields

Use dots to reach inside objects. A path into a list applies to every item.

Given an action that returns:

```json
{
  "id": 42,
  "name": "Ada",
  "email": "ada@example.com",
  "address": { "street": "12 St James's Sq", "city": "London", "zip": "SW1Y" },
  "orders": [
    { "id": 1, "total": 10.5, "status": "paid" },
    { "id": 2, "total": 99, "status": "open" }
  ]
}
```

Request:

```http
GET /api/customers/42?fields=id,address.city,orders.total
```

Response:

```json
{
  "id": 42,
  "address": { "city": "London" },
  "orders": [
    { "total": 10.5 },
    { "total": 99 }
  ]
}
```

Asking for `address` returns the whole object, even if `address.city` is also in the list:

```http
GET /api/customers/42?fields=address,address.city
```

Response:

```json
{
  "address": { "street": "12 St James's Sq", "city": "London", "zip": "SW1Y" }
}
```

### Lists

When the action returns a list (including `IAsyncEnumerable<T>`), the fields apply to every item. Single values such as a number or `null` come back unchanged.

## What it does (and doesn't) save

WhatFYN trims the **response payload**: less data over the wire and less for the client to parse. It does **not** make the server do less work. The action still loads and builds the full object, all four hundred fields, before the formatter throws most of them away.

If the real cost is in the query itself, filter at the data layer instead:

- a dynamic `Select` projection in Entity Framework,
- [OData](https://learn.microsoft.com/odata/) `$select`,
- [GraphQL](https://graphql.org/).

WhatFYN is meant as the lightweight option. It has no extra dependencies, works with the controllers you already have, and fits when bandwidth or client-side parsing is the bottleneck.

## Limitations

- Serialization always uses System.Text.Json. An app that switched MVC to Newtonsoft.Json still gets System.Text.Json output for filtered responses, so Newtonsoft attributes such as `[JsonProperty]` are ignored there.

## Development

```sh
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~FieldFilterTests.Returns_only_the_requested_fields"
```

## License

[MIT](https://github.com/georgepaoli/WhatFYN/blob/master/LICENSE)
