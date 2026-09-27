using System.Text.Json;

namespace WhatFYN;

/// <summary>
/// The fields a request asks for, as a tree of dotted paths (<c>id,address.city,orders.total</c>).
/// </summary>
internal sealed class FieldSelection
{
    private static readonly char[] Separators = [',', ';'];

    // A null child means the whole value is selected.
    private readonly Dictionary<string, FieldSelection?> _fields = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Parses a field list; null when it names no fields.
    /// </summary>
    public static FieldSelection? Parse(string value)
    {
        var selection = new FieldSelection();

        foreach (var path in value.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length > 0)
                selection.Add(segments);
        }

        return selection._fields.Count > 0 ? selection : null;
    }

    private void Add(ReadOnlySpan<string> segments)
    {
        var name = segments[0];

        if (segments.Length == 1)
        {
            _fields[name] = null;
            return;
        }

        if (_fields.TryGetValue(name, out var child))
        {
            // The whole value is already selected, which covers any sub-path.
            if (child is null)
                return;
        }
        else
        {
            child = new FieldSelection();
            _fields[name] = child;
        }

        child.Add(segments[1..]);
    }

    /// <summary>
    /// Copies <paramref name="json"/> to <paramref name="writer"/>, leaving out every property the selection
    /// doesn't name. Arrays are filtered item by item; other values are copied as they are.
    /// </summary>
    public void WriteFiltered(ReadOnlySpan<byte> json, Utf8JsonWriter writer, JsonReaderOptions readerOptions)
    {
        var reader = new Utf8JsonReader(json, readerOptions);
        reader.Read();
        WriteValue(ref reader, writer, json, this);
    }

    // Writes the value the reader is on and leaves the reader on its last token.
    // A null selection keeps the whole value.
    private static void WriteValue(ref Utf8JsonReader reader, Utf8JsonWriter writer, ReadOnlySpan<byte> json, FieldSelection? selection)
    {
        var isContainer = reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray;

        // Kept whole: copy the bytes as they are. Indented output needs re-writing so nesting lines up.
        if (!isContainer || (selection is null && !writer.Options.Indented))
        {
            var start = (int)reader.TokenStartIndex;
            reader.Skip();
            writer.WriteRawValue(json[start..(int)reader.BytesConsumed], skipInputValidation: true);
            return;
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            writer.WriteStartArray();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                WriteValue(ref reader, writer, json, selection);
            writer.WriteEndArray();
            return;
        }

        writer.WriteStartObject();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var name = reader.GetString()!;
            reader.Read();

            FieldSelection? child = null;
            if (selection is null || selection._fields.TryGetValue(name, out child))
            {
                writer.WritePropertyName(name);
                WriteValue(ref reader, writer, json, child);
            }
            else
            {
                reader.Skip();
            }
        }
        writer.WriteEndObject();
    }
}
