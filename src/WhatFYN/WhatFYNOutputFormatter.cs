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
        return GetRequestedFields(context.HttpContext.Request) is not null
            && base.CanWriteResult(context);
    }

    /// <inheritdoc />
    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var node = JsonSerializer.SerializeToNode(context.Object, context.ObjectType ?? typeof(object), _serializerOptions);

        if (node is JsonObject root)
            GetRequestedFields(context.HttpContext.Request)!.Apply(root);

        return context.HttpContext.Response.WriteAsync(
            node?.ToJsonString(_serializerOptions) ?? "null",
            selectedEncoding,
            context.HttpContext.RequestAborted);
    }

    // The fields from the header or, failing that, the query string; null when neither lists any.
    private FieldSelection? GetRequestedFields(HttpRequest request)
    {
        return FieldSelection.Parse(request.Headers[_options.HeaderName].ToString())
            ?? FieldSelection.Parse(request.Query[_options.QueryParameterName].ToString());
    }
}
