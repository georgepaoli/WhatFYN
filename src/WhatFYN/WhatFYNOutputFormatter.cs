using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace WhatFYN;

/// <summary>
/// JSON output formatter that returns only the fields the client lists in the request.
/// It handles a response only when the request asks for fields; otherwise the next JSON formatter does.
/// </summary>
public sealed class WhatFYNOutputFormatter : TextOutputFormatter
{
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly WhatFYNOptions _options;

    /// <summary>
    /// Creates the formatter.
    /// </summary>
    /// <param name="serializerOptions">Options used to serialize the response.</param>
    /// <param name="options">WhatFYN options.</param>
    public WhatFYNOutputFormatter(JsonSerializerOptions serializerOptions, WhatFYNOptions options)
    {
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        SupportedMediaTypes.Add("application/json");
        SupportedMediaTypes.Add("text/json");
        SupportedMediaTypes.Add("application/*+json");

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    /// <inheritdoc />
    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        return GetFieldList(context.HttpContext.Request) is not null
            && base.CanWriteResult(context);
    }

    /// <inheritdoc />
    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var node = JsonSerializer.SerializeToNode(context.Object, context.ObjectType ?? typeof(object), _serializerOptions);

        if (node is JsonObject root)
        {
            var fields = GetFieldList(context.HttpContext.Request)!.Split(';');

            var keysToRemove = root.Select(p => p.Key).Except(fields).ToList();

            foreach (var key in keysToRemove)
                root.Remove(key);
        }

        return context.HttpContext.Response.WriteAsync(
            node?.ToJsonString(_serializerOptions) ?? "null",
            selectedEncoding,
            context.HttpContext.RequestAborted);
    }

    // The raw field list from the header or, failing that, the query string; null when neither has one.
    private string? GetFieldList(HttpRequest request)
    {
        var header = request.Headers[_options.HeaderName].ToString();
        if (!string.IsNullOrWhiteSpace(header))
            return header;

        var query = request.Query[_options.QueryParameterName].ToString();
        if (!string.IsNullOrWhiteSpace(query))
            return query;

        return null;
    }
}
