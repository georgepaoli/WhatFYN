using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace WhatFYN.Tests;

public record Address(string Street, string City, string Zip);

public record Order(int Id, decimal Total, string Status);

public record Customer(int Id, string Name, string Email, Address Address, Order[] Orders);

public static class Samples
{
    public static Customer Ada { get; } = new(
        42, "Ada", "ada@example.com",
        new Address("12 St James's Sq", "London", "SW1Y"),
        [new Order(1, 10.5m, "paid"), new Order(2, 99m, "open")]);

    public static Customer Alan { get; } = new(
        7, "Alan", "alan@example.com",
        new Address("Bletchley Park", "Milton Keynes", "MK3"),
        []);
}

[ApiController]
[Route("customers")]
public class CustomersController : ControllerBase
{
    [HttpGet("one")]
    public Customer One() => Samples.Ada;

    [HttpGet("many")]
    public Customer[] Many() => [Samples.Ada, Samples.Alan];

    [HttpGet("text")]
    public string Text() => "hello";

    [HttpGet("number")]
    public int Number() => 42;
}

/// <summary>
/// In-memory app with controllers and WhatFYN registered.
/// </summary>
public sealed class TestApp : IAsyncDisposable
{
    private readonly WebApplication _app;

    private TestApp(WebApplication app)
    {
        _app = app;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }

    public static async Task<TestApp> StartAsync(Action<WhatFYNOptions>? configure = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(TestApp).Assembly)
            .AddWhatFYN(configure);

        var app = builder.Build();
        app.MapControllers();
        await app.StartAsync();

        return new TestApp(app);
    }

    public async Task<HttpResponseMessage> GetAsync(string path, string? fields = null, string headerName = "x-only-fields")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (fields is not null)
            request.Headers.Add(headerName, fields);

        var response = await Client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response;
    }

    public async Task<JsonNode?> GetJsonAsync(string path, string? fields = null, string headerName = "x-only-fields")
    {
        var response = await GetAsync(path, fields, headerName);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync());
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}

public static class JsonAssert
{
    public static void HasExactly(JsonNode? node, params string[] keys)
    {
        var obj = Assert.IsType<JsonObject>(node);
        Assert.Equal(keys.OrderBy(k => k), obj.Select(p => p.Key).OrderBy(k => k));
    }
}
