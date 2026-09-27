using System.Text.Json.Nodes;

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
    /// Removes every property the selection doesn't name. Arrays are applied item by item;
    /// other values are left alone.
    /// </summary>
    public void Apply(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (!_fields.TryGetValue(key, out var child))
                        obj.Remove(key);
                    else
                        child?.Apply(obj[key]);
                }
                break;

            case JsonArray array:
                foreach (var item in array)
                    Apply(item);
                break;
        }
    }
}
