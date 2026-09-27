using System.Text.Json.Nodes;

namespace WhatFYN.Tests;

public class FieldFilterTests
{
    [Fact]
    public async Task Without_fields_returns_the_full_object()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one");

        JsonAssert.HasExactly(json, "id", "name", "email", "address", "orders");
    }

    [Fact]
    public async Task Returns_only_the_requested_fields()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one", "id;name");

        JsonAssert.HasExactly(json, "id", "name");
        Assert.Equal(42, (int)json!["id"]!);
        Assert.Equal("Ada", (string)json["name"]!);
    }

    [Fact]
    public async Task Requested_object_field_comes_back_whole()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one", "address");

        JsonAssert.HasExactly(json, "address");
        JsonAssert.HasExactly(json!["address"], "street", "city", "zip");
    }

    [Fact]
    public async Task Responds_with_json_content_type()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetAsync("customers/one", "id");

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Leaves_string_results_to_the_string_formatter()
    {
        await using var app = await TestApp.StartAsync();

        var response = await app.GetAsync("customers/text", "id");

        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("hello", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Writes_primitive_results_unchanged()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/number", "id");

        Assert.Equal(42, json!.GetValue<int>());
    }

    [Fact]
    public async Task Empty_header_returns_the_full_object()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one", " ");

        JsonAssert.HasExactly(json, "id", "name", "email", "address", "orders");
    }

    [Fact]
    public async Task Reads_fields_from_the_query_string()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one?fields=id;name");

        JsonAssert.HasExactly(json, "id", "name");
    }

    [Fact]
    public async Task Header_wins_over_the_query_string()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one?fields=id", "name");

        JsonAssert.HasExactly(json, "name");
    }

    [Fact]
    public async Task Query_parameter_name_is_configurable()
    {
        await using var app = await TestApp.StartAsync(o => o.QueryParameterName = "select");

        var json = await app.GetJsonAsync("customers/one?select=email");

        JsonAssert.HasExactly(json, "email");
    }

    [Theory]
    [InlineData("id,name")]
    [InlineData("id;name")]
    [InlineData(" id , name ")]
    [InlineData("id,;name,")]
    public async Task Accepts_comma_or_semicolon_separators(string fields)
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one", fields);

        JsonAssert.HasExactly(json, "id", "name");
    }

    [Fact]
    public async Task Field_names_are_case_insensitive()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one?fields=ID,Name,EMAIL");

        JsonAssert.HasExactly(json, "id", "name", "email");
    }

    [Fact]
    public async Task Only_separators_returns_the_full_object()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one", ",;,");

        JsonAssert.HasExactly(json, "id", "name", "email", "address", "orders");
    }

    [Fact]
    public async Task Header_name_is_configurable()
    {
        await using var app = await TestApp.StartAsync(o => o.HeaderName = "x-fields");

        var json = await app.GetJsonAsync("customers/one", "id", headerName: "x-fields");

        JsonAssert.HasExactly(json, "id");
    }
}
