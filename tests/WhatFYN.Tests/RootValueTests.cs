using System.Text.Json.Nodes;

namespace WhatFYN.Tests;

public class RootValueTests
{
    [Theory]
    [InlineData("customers/many")]
    [InlineData("customers/stream")]
    public async Task Filters_every_item_of_a_list(string path)
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync(path + "?fields=id,name");

        var items = Assert.IsType<JsonArray>(json);
        Assert.Equal(2, items.Count);
        Assert.All(items, item => JsonAssert.HasExactly(item, "id", "name"));
        Assert.Equal("Alan", (string)items[1]!["name"]!);
    }

    [Fact]
    public async Task Applies_nested_paths_to_every_item_of_a_list()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/many?fields=address.city");

        var items = Assert.IsType<JsonArray>(json);
        Assert.All(items, item => JsonAssert.HasExactly(item!["address"], "city"));
    }

    [Fact]
    public async Task Returns_an_empty_list_unchanged()
    {
        await using var app = await TestApp.StartAsync();

        var json = await app.GetJsonAsync("customers/empty?fields=id");

        Assert.Empty(Assert.IsType<JsonArray>(json));
    }
}
