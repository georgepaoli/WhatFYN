namespace WhatFYN;

/// <summary>
/// Options for the WhatFYN field filter.
/// </summary>
public sealed class WhatFYNOptions
{
    /// <summary>
    /// Request header that lists the fields to return. Defaults to <c>x-only-fields</c>.
    /// </summary>
    public string HeaderName { get; set; } = "x-only-fields";

    /// <summary>
    /// Query string parameter that lists the fields to return. Defaults to <c>fields</c>.
    /// The header wins when a request carries both.
    /// </summary>
    public string QueryParameterName { get; set; } = "fields";
}
