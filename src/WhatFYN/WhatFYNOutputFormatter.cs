using System.Buffers;
using System.Text;
using System.Text.Json;
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
    private readonly JsonReaderOptions _readerOptions;
    private readonly JsonWriterOptions _writerOptions;

    // HttpContext.Items key for the request's parsed field list, so it's parsed once per request.
    private readonly object _selectionKey = new();

    /// <summary>
    /// Creates the formatter.
    /// </summary>
    /// <param name="serializerOptions">Options used to serialize the response.</param>
    /// <param name="options">WhatFYN options.</param>
    public WhatFYNOutputFormatter(JsonSerializerOptions serializerOptions, WhatFYNOptions options)
    {
        _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        _readerOptions = new JsonReaderOptions { MaxDepth = serializerOptions.MaxDepth };
        _writerOptions = new JsonWriterOptions
        {
            Encoder = serializerOptions.Encoder,
            Indented = serializerOptions.WriteIndented,
            SkipValidation = true,
#if NET9_0_OR_GREATER
            IndentCharacter = serializerOptions.IndentCharacter,
            IndentSize = serializerOptions.IndentSize,
            NewLine = serializerOptions.NewLine,
#endif
        };

        SupportedMediaTypes.Add("application/json");
        SupportedMediaTypes.Add("text/json");
        SupportedMediaTypes.Add("application/*+json");

        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    /// <inheritdoc />
    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        return GetRequestedFields(context.HttpContext) is not null
            && base.CanWriteResult(context);
    }

    /// <inheritdoc />
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var httpContext = context.HttpContext;
        var selection = GetRequestedFields(httpContext)!;
        var json = await SerializeAsync(context.Object, context.ObjectType ?? typeof(object), httpContext.RequestAborted);

        if (selectedEncoding.CodePage == Encoding.UTF8.CodePage)
        {
            await using var writer = new Utf8JsonWriter(httpContext.Response.Body, _writerOptions);
            selection.WriteFiltered(json.Span, writer, _readerOptions);
            await writer.FlushAsync(httpContext.RequestAborted);
            return;
        }

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, _writerOptions))
            selection.WriteFiltered(json.Span, writer, _readerOptions);

        await httpContext.Response.WriteAsync(
            Encoding.UTF8.GetString(buffer.WrittenSpan),
            selectedEncoding,
            httpContext.RequestAborted);
    }

    private async Task<ReadOnlyMemory<byte>> SerializeAsync(object? value, Type type, CancellationToken cancellationToken)
    {
        if (!IsAsyncEnumerable(type))
            return JsonSerializer.SerializeToUtf8Bytes(value, type, _serializerOptions);

        // System.Text.Json only serializes IAsyncEnumerable<T> asynchronously.
        var buffer = new MemoryStream();
        await JsonSerializer.SerializeAsync(buffer, value, type, _serializerOptions, cancellationToken);
        return buffer.GetBuffer().AsMemory(0, (int)buffer.Length);
    }

    private static bool IsAsyncEnumerable(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)
            || type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
    }

    // The fields from the header or, failing that, the query string; null when neither lists any.
    private FieldSelection? GetRequestedFields(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(_selectionKey, out var cached))
            return (FieldSelection?)cached;

        var request = httpContext.Request;
        var selection = FieldSelection.Parse(request.Headers[_options.HeaderName].ToString())
            ?? FieldSelection.Parse(request.Query[_options.QueryParameterName].ToString());

        httpContext.Items[_selectionKey] = selection;
        return selection;
    }
}
