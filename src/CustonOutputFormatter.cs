using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace WhatFYN
{
    public class CustomOutputFormatter : TextOutputFormatter
    {
        private static readonly JsonSerializerSettings DefaultJsonSerializerSettings
            = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            };

        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public CustomOutputFormatter()
            : this(DefaultJsonSerializerSettings, DefaultEncoding)
        {

        }

        public CustomOutputFormatter(JsonSerializerSettings settings)
            : this(settings, DefaultEncoding)
        {
        }

        public CustomOutputFormatter(Encoding encoding)
            : this(DefaultJsonSerializerSettings, encoding)
        {
        }

        public CustomOutputFormatter(JsonSerializerSettings settings, Encoding encoding)
        {
            _jsonSerializerSettings = settings;

            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/x-wfyn+json"));
            SupportedEncodings.Add(encoding);
        }

        protected override bool CanWriteType(Type type)
        {
            return true;
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (!context.HttpContext.Request.Headers.ContainsKey("x-only-fields"))
                throw new InvalidOperationException("Header 'x-only-fields' is required to Accept type.");

            var root = JObject.Parse(JsonConvert.SerializeObject(context.Object, _jsonSerializerSettings));

            var keys = root.Properties().Select(p => p.Name).ToList();
            
            var fields = context.HttpContext.Request.Headers["x-only-fields"].ToString().Split(";");
            
            var keysToRemove = keys.Except(fields);
            
            foreach (var k in keysToRemove)
                root.Property(k).Remove();

            return context.HttpContext.Response.WriteAsync(
                JsonConvert.SerializeObject(root),
                selectedEncoding,
                context.HttpContext.RequestAborted);
        }
    }
}