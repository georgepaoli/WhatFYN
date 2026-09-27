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
}
