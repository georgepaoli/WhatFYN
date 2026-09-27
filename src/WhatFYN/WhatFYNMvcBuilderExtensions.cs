using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using WhatFYN;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers WhatFYN with MVC.
/// </summary>
public static class WhatFYNMvcBuilderExtensions
{
    /// <summary>
    /// Adds the WhatFYN output formatter, which trims JSON responses to the fields the client asks for.
    /// It serializes with the app's MVC <see cref="JsonOptions"/>.
    /// </summary>
    public static IMvcBuilder AddWhatFYN(this IMvcBuilder builder, Action<WhatFYNOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<WhatFYNOptions>();
        if (configure is not null)
            builder.Services.Configure(configure);

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, WhatFYNMvcOptionsSetup>());

        return builder;
    }

    private sealed class WhatFYNMvcOptionsSetup(
        IOptions<JsonOptions> jsonOptions,
        IOptions<WhatFYNOptions> options) : IConfigureOptions<MvcOptions>
    {
        public void Configure(MvcOptions mvcOptions)
        {
            var formatter = new WhatFYNOutputFormatter(jsonOptions.Value.JsonSerializerOptions, options.Value);

            // Go right before the app's JSON formatter, so string and stream results keep their own formatters.
            var formatters = mvcOptions.OutputFormatters;
            for (var i = 0; i < formatters.Count; i++)
            {
                if (formatters[i] is OutputFormatter f && f.SupportedMediaTypes.Contains("application/json"))
                {
                    formatters.Insert(i, formatter);
                    return;
                }
            }

            formatters.Add(formatter);
        }
    }
}
