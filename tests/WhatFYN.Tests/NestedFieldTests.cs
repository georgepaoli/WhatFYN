using System.Text.Json.Nodes;

namespace WhatFYN.Tests;

public class NestedFieldTests
{
    [Fact]
    public async Task Selects_a_field_inside_an_object()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one?fields=id,address.city");

        JsonAssert.HasExactly(json, "id", "address");
        JsonAssert.HasExactly(json!["address"], "city");
        Assert.Equal("London", (string)json["address"]!["city"]!);
    }

    [Fact]
    public async Task Merges_sibling_paths()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one?fields=address.city,address.zip");

        JsonAssert.HasExactly(json!["address"], "city", "zip");
    }

    [Theory]
    [InlineData("address,address.city")]
    [InlineData("address.city,address")]
    public async Task Whole_object_wins_over_a_sub_path(string fields)
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one", fields);

        JsonAssert.HasExactly(json!["address"], "street", "city", "zip");
    }

    [Fact]
    public async Task Selects_a_field_in_every_item_of_a_nested_array()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one?fields=orders.total");

        JsonAssert.HasExactly(json, "orders");
        var orders = Assert.IsType<JsonArray>(json!["orders"]);
        Assert.Equal(2, orders.Count);
        Assert.All(orders, order => JsonAssert.HasExactly(order, "total"));
    }

    [Fact]
    public async Task Nested_names_are_case_insensitive()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one?fields=Address.CITY");

        JsonAssert.HasExactly(json!["address"], "city");
    }

    [Fact]
    public async Task Path_into_a_plain_value_returns_the_value()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/one?fields=name.first");

        JsonAssert.HasExactly(json, "name");
        Assert.Equal("Ada", (string)json!["name"]!);
    }
}
