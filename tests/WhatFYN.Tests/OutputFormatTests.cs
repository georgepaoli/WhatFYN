using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WhatFYN.Tests;

public class OutputFormatTests
{
    [Theory]
    [InlineData("title,body,place")]
    [InlineData("title,body,place.city")]
    public async Task Keeps_escaped_and_non_ascii_strings_intact(string fields)
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/note", fields);

        Assert.Equal("Olá \"José\" & <co>", (string)json!["title"]!);
        Assert.Equal("line1\nline2\ttab 😀", (string)json["body"]!);
        Assert.Equal("São Paulo", (string)json["place"]!["city"]!);
    }

    [Fact]
    public async Task Keeps_null_values_that_were_asked_for()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/note", "title,missing");

        JsonAssert.HasExactly(json, "title", "missing");
        Assert.Null(json!["missing"]);
    }

    [Fact]
    public async Task Writes_indented_output_when_the_app_does()
    {
        await using var app = await TestApp.StartAsync(configureJson: o => o.JsonSerializerOptions.WriteIndented = true);

        var response = await app.GetAsync("customers/one", "id,address,orders.total");
        var body = await response.Content.ReadAsStringAsync();

        var expected = JsonSerializer.Serialize(
            new
            {
                id = 42,
                address = new { street = "12 St James's Sq", city = "London", zip = "SW1Y" },
                orders = new[] { new { total = 10.5m }, new { total = 99m } },
            },
            new JsonSerializerOptions { WriteIndented = true });
        Assert.Equal(expected, body);
    }

    [Fact]
    public async Task Follows_the_app_naming_policy()
    {
        await using var app = await TestApp.StartAsync(
            configureJson: o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);

        var json = await app.GetJsonAsync("customers/note", "title,place.zip");

        JsonAssert.HasExactly(json, "title", "place");
        JsonAssert.HasExactly(json!["place"], "zip");
    }

    [Fact]
    public async Task Writes_utf16_when_the_client_asks_for_it()
    {
        await using var app = await TestApp.StartAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, "customers/note?fields=title,place.city");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-16"));
        var response = await app.Client.SendAsync(request);

        Assert.Equal("utf-16", response.Content.Headers.ContentType?.CharSet);
        var body = Encoding.Unicode.GetString(await response.Content.ReadAsByteArrayAsync());
        var json = JsonNode.Parse(body);
        JsonAssert.HasExactly(json, "title", "place");
        Assert.Equal("Olá \"José\" & <co>", (string)json!["title"]!);
    }
}
