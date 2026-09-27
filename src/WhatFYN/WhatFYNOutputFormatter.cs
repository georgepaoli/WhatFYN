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
        {
            var fields = GetRequestedFields(context.HttpContext.Request)!;

            var keysToRemove = root.Select(p => p.Key).Where(k => !fields.Contains(k)).ToList();

            foreach (var key in keysToRemove)
                root.Remove(key);
        }

        return context.HttpContext.Response.WriteAsync(
            node?.ToJsonString(_serializerOptions) ?? "null",
            selectedEncoding,
            context.HttpContext.RequestAborted);
    }

    // The fields from the header or, failing that, the query string; null when neither lists any.
    private HashSet<string>? GetRequestedFields(HttpRequest request)
    {
        return ParseFields(request.Headers[_options.HeaderName].ToString())
            ?? ParseFields(request.Query[_options.QueryParameterName].ToString());
    }

    private static readonly char[] Separators = [',', ';'];

    private static HashSet<string>? ParseFields(string value)
    {
        var fields = new HashSet<string>(
            value.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StringComparer.OrdinalIgnoreCase);

        return fields.Count > 0 ? fields : null;
    }
}
