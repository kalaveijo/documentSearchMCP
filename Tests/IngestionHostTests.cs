using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using App.Clients;
using App.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Tests;

[TestClass]
public sealed class IngestionHostTests
{
    [TestMethod]
    [DataRow("{}")]
    [DataRow("""{"text":null}""")]
    [DataRow("""{"text":""}""")]
    [DataRow("""{"text":"  \t"}""")]
    [DataRow("""{"text":123}""")]
    [DataRow("""{"text":""")]
    public async Task PostInvalidText_ReturnsBadRequest(string body)
    {
        var stub = new StubEmbeddingClient();
        await using var factory = CreateFactory(stub);
        using var client = factory.CreateClient();
        using var response = await client.PostAsync(
            "/v1/ingest",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.IsNull(stub.ReceivedText);
    }

    [TestMethod]
    public async Task PostText_ReturnsCommonDimensionAndVectorCount()
    {
        var stub = new StubEmbeddingClient { Vectors = [new float[] { 1, 2 }, new float[] { 3, 4 }] };
        await using var factory = CreateFactory(stub);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/v1/ingest", new { text = "input text" });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("input text", stub.ReceivedText);
        var result = await response.Content.ReadFromJsonAsync<IngestionResponse>();
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.EmbeddingSize);
        Assert.AreEqual(2L, result.EmbeddingCount);
    }

    [TestMethod]
    [DataRow(false, HttpStatusCode.BadGateway)]
    [DataRow(true, HttpStatusCode.GatewayTimeout)]
    public async Task PostText_ReportsEmbeddingFailure(bool isTimeout, HttpStatusCode status)
    {
        var stub = new StubEmbeddingClient
        {
            Error = new EmbeddingException("private upstream detail", isTimeout)
        };
        await using var factory = CreateFactory(stub);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/v1/ingest", new { text = "input text" });

        Assert.AreEqual(status, response.StatusCode);
        Assert.DoesNotContain("private upstream detail", await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task PostText_RejectsInconsistentVectors()
    {
        var stub = new StubEmbeddingClient { Vectors = [new float[] { 1 }, new float[] { 1, 2 }] };
        await using var factory = CreateFactory(stub);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/v1/ingest", new { text = "input text" });

        Assert.AreEqual(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [TestMethod]
    public async Task PostText_RejectsMissingVectors()
    {
        var stub = new StubEmbeddingClient { Vectors = [] };
        await using var factory = CreateFactory(stub);
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/v1/ingest", new { text = "input text" });

        Assert.AreEqual(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [TestMethod]
    public async Task PostLargeText_ExceedsDefaultKestrelBodyLimit()
    {
        var text = new string('x', 30_000_001);
        var stub = new StubEmbeddingClient();
        await using var factory = CreateFactory(stub);
        using var client = factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);
        using var response = await client.PostAsJsonAsync("/v1/ingest", new { text });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(stub.ReceivedText);
        Assert.AreEqual(text.Length, stub.ReceivedText.Length);
        Assert.IsTrue(text.AsSpan().SequenceEqual(stub.ReceivedText.AsSpan()));
    }

    [TestMethod]
    [DataRow("Ollama:BaseUrl", "ftp://ollama.test")]
    [DataRow("Ollama:BaseUrl", "http://ollama.test/api")]
    [DataRow("Ollama:Model", " ")]
    [DataRow("Ollama:TimeoutSeconds", "0")]
    public async Task InvalidOllamaConfiguration_FailsAtStartup(string key, string value)
    {
        await using var factory = CreateFactory(
            new StubEmbeddingClient(),
            new Dictionary<string, string?> { [key] = value });

        Assert.ThrowsExactly<OptionsValidationException>(() => factory.CreateClient());
    }

    private static StubFactory CreateFactory(
        StubEmbeddingClient stub,
        Dictionary<string, string?>? configuration = null)
    {
        var factory = new StubFactory(stub, configuration);
        factory.UseKestrel(0);
        return factory;
    }

    private sealed class StubFactory(
        StubEmbeddingClient stub,
        Dictionary<string, string?>? configuration) : WebApplicationFactory<global::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            if (configuration is not null)
            {
                builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(configuration));
            }

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmbeddingClient>();
                services.AddSingleton<IEmbeddingClient>(stub);
            });
        }
    }

    private sealed class StubEmbeddingClient : IEmbeddingClient
    {
        public string? ReceivedText { get; private set; }
        public float[][] Vectors { get; init; } = [new float[] { 1, 2 }];
        public EmbeddingException? Error { get; init; }

        public async IAsyncEnumerable<EmbeddingChunk> CreateEmbeddingsAsync(
            string text,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ReceivedText = text;
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            if (Error is not null)
            {
                throw Error;
            }

            foreach (var vector in Vectors)
            {
                yield return new EmbeddingChunk(text, vector);
            }
        }
    }
}
