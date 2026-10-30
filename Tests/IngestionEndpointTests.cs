using System.Net;
using System.Net.Http.Json;
using App.Controllers;

namespace Tests;

[TestClass]
public sealed class IngestionEndpointTests
{
    [TestMethod]
    public async Task PostShortText_ReturnsEmbeddingSize()
    {
        using var client = TestApiClient.CreateClient(TimeSpan.FromMinutes(5));
        using var response = await client.PostAsJsonAsync("/v1/ingest", new { text = "A short document for embedding." });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<IngestionResponse>();
        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.EmbeddingSize);
        Assert.AreEqual(1L, result.EmbeddingCount);
    }

    [TestMethod]
    public async Task PostLongText_ReturnsMultipleEmbeddings()
    {
        using var client = TestApiClient.CreateClient(TimeSpan.FromMinutes(5));
        var text = string.Concat(Enumerable.Repeat("A document with words and whitespace. ", 200));
        using var response = await client.PostAsJsonAsync("/v1/ingest", new { text });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<IngestionResponse>();
        Assert.IsNotNull(result);
        Assert.IsGreaterThan(0, result.EmbeddingSize);
        Assert.IsGreaterThan(1L, result.EmbeddingCount);
    }
}
