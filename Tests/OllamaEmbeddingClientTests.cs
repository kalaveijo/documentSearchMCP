using System.Net;
using System.Text;
using System.Text.Json;
using App.Clients;
using Microsoft.Extensions.Options;

namespace Tests;

[TestClass]
public sealed class OllamaEmbeddingClientTests
{
    [TestMethod]
    public async Task CreateEmbeddings_SendsNativeRequestWithoutTruncation()
    {
        using var handler = new StubHandler(async (request, cancellationToken) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("http://ollama.test/api/embed", request.RequestUri?.ToString());
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Assert.AreEqual("embeddinggemma", json.RootElement.GetProperty("model").GetString());
            Assert.AreEqual("hello", json.RootElement.GetProperty("input").GetString());
            Assert.IsFalse(json.RootElement.GetProperty("truncate").GetBoolean());
            return Success(1, 2, 3);
        });
        using var httpClient = CreateHttpClient(handler);
        var client = CreateClient(httpClient);

        var chunks = await CollectAsync(client, "hello");

        Assert.HasCount(1, chunks);
        Assert.AreEqual("hello", chunks[0].Text);
        CollectionAssert.AreEqual(new float[] { 1, 2, 3 }, chunks[0].Vector);
    }

    [TestMethod]
    public async Task CreateEmbeddings_PreservesLongTextWhitespaceAndUnicode()
    {
        var text = new string('a', 2047) + "😀" + new string('b', 2100)
            + "\n   one two\tthree 😀 " + new string('c', 3000);
        var submitted = new List<string>();
        using var handler = new StubHandler(async (request, cancellationToken) =>
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            submitted.Add(json.RootElement.GetProperty("input").GetString()!);
            return Success(1, 2);
        });
        using var httpClient = CreateHttpClient(handler);
        var chunks = await CollectAsync(CreateClient(httpClient), text);

        Assert.IsGreaterThan(1, chunks.Count);
        Assert.AreEqual(text, string.Concat(chunks.Select(chunk => chunk.Text)));
        CollectionAssert.AreEqual(submitted, chunks.Select(chunk => chunk.Text).ToList());
        Assert.IsTrue(chunks.All(chunk => chunk.Text.EnumerateRunes().Count() <= 2048));
    }

    [TestMethod]
    public async Task CreateEmbeddings_SplitsOnlyContextErrorsAndPreservesOrder()
    {
        const string text = "ab😀c defgh";
        var attempted = new List<string>();
        using var handler = new StubHandler(async (request, cancellationToken) =>
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var input = json.RootElement.GetProperty("input").GetString()!;
            attempted.Add(input);
            return input.EnumerateRunes().Count() > 3
                ? JsonResponse(HttpStatusCode.BadRequest, """{"error":"input exceeds maximum context length"}""")
                : Success(1, 2);
        });
        using var httpClient = CreateHttpClient(handler);

        var chunks = await CollectAsync(CreateClient(httpClient), text);

        Assert.AreEqual(text, string.Concat(chunks.Select(chunk => chunk.Text)));
        Assert.IsTrue(chunks.All(chunk => chunk.Text.EnumerateRunes().Count() <= 3));
        Assert.IsGreaterThan(chunks.Count, attempted.Count);
    }

    [TestMethod]
    public async Task CreateEmbeddings_UnsplittableContextErrorFails()
    {
        var attempts = 0;
        using var handler = new StubHandler((_, _) =>
        {
            attempts++;
            return Task.FromResult(JsonResponse(
                HttpStatusCode.BadRequest,
                """{"error":"input exceeds maximum context length"}"""));
        });
        using var httpClient = CreateHttpClient(handler);

        await Assert.ThrowsExactlyAsync<EmbeddingException>(() => CollectAsync(CreateClient(httpClient), "😀"));
        Assert.AreEqual(1, attempts);
    }

    [TestMethod]
    public async Task CreateEmbeddings_OtherBadRequestDoesNotSplit()
    {
        var attempts = 0;
        using var handler = new StubHandler((_, _) =>
        {
            attempts++;
            return Task.FromResult(JsonResponse(HttpStatusCode.BadRequest, """{"error":"other problem"}"""));
        });
        using var httpClient = CreateHttpClient(handler);

        await Assert.ThrowsExactlyAsync<EmbeddingException>(() => CollectAsync(CreateClient(httpClient), "hello"));
        Assert.AreEqual(1, attempts);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.NotFound, """{"error":"model not found"}""")]
    [DataRow(HttpStatusCode.InternalServerError, """{"error":"server error"}""")]
    [DataRow(HttpStatusCode.OK, "not json")]
    [DataRow(HttpStatusCode.OK, """{"embeddings":[]}""")]
    [DataRow(HttpStatusCode.OK, """{"embeddings":[[]]}""")]
    [DataRow(HttpStatusCode.OK, """{"embeddings":[[1],[2]]}""")]
    [DataRow(HttpStatusCode.OK, """{"embeddings":[[NaN]]}""")]
    public async Task CreateEmbeddings_RejectsUpstreamErrorsAndInvalidVectors(
        HttpStatusCode status,
        string body)
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(JsonResponse(status, body)));
        using var httpClient = CreateHttpClient(handler);

        await Assert.ThrowsExactlyAsync<EmbeddingException>(() => CollectAsync(CreateClient(httpClient), "hello"));
    }

    [TestMethod]
    public async Task CreateEmbeddings_RejectsInconsistentDimensions()
    {
        var calls = 0;
        using var handler = new StubHandler((_, _) => Task.FromResult(++calls == 1
            ? Success(1)
            : Success(1, 2)));
        using var httpClient = CreateHttpClient(handler);

        await Assert.ThrowsExactlyAsync<EmbeddingException>(
            () => CollectAsync(CreateClient(httpClient), new string('x', 2049)));
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public async Task CreateEmbeddings_RejectsInvalidUnicode()
    {
        using var handler = new StubHandler((_, _) => Task.FromResult(Success(1)));
        using var httpClient = CreateHttpClient(handler);

        await Assert.ThrowsExactlyAsync<EmbeddingInputException>(
            () => CollectAsync(CreateClient(httpClient), "invalid \ud800 text"));
    }

    [TestMethod]
    public async Task CreateEmbeddings_PropagatesCallerCancellation()
    {
        using var handler = new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Success(1);
        });
        using var httpClient = CreateHttpClient(handler);
        using var cancellation = new CancellationTokenSource();
        var task = CollectAsync(CreateClient(httpClient), "hello", cancellation.Token);
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
    }

    [TestMethod]
    public async Task CreateEmbeddings_ReportsUpstreamTimeout()
    {
        using var handler = new StubHandler((_, _) => throw new TaskCanceledException());
        using var httpClient = CreateHttpClient(handler);

        var exception = await Assert.ThrowsExactlyAsync<EmbeddingException>(
            () => CollectAsync(CreateClient(httpClient), "hello"));
        Assert.IsTrue(exception.IsTimeout);
    }

    [TestMethod]
    public async Task CreateEmbeddings_ReportsConnectionFailure()
    {
        using var handler = new StubHandler((_, _) => throw new HttpRequestException("private upstream detail"));
        using var httpClient = CreateHttpClient(handler);

        var exception = await Assert.ThrowsExactlyAsync<EmbeddingException>(
            () => CollectAsync(CreateClient(httpClient), "hello"));
        Assert.IsFalse(exception.IsTimeout);
        Assert.DoesNotContain("private upstream detail", exception.Message);
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("http://ollama.test/") };

    private static OllamaEmbeddingClient CreateClient(HttpClient httpClient) =>
        new(httpClient, Options.Create(new OllamaOptions()));

    private static async Task<List<EmbeddingChunk>> CollectAsync(
        IEmbeddingClient client,
        string text,
        CancellationToken cancellationToken = default)
    {
        var chunks = new List<EmbeddingChunk>();
        await foreach (var chunk in client.CreateEmbeddingsAsync(text, cancellationToken))
        {
            chunks.Add(chunk);
        }

        return chunks;
    }

    private static HttpResponseMessage Success(params float[] vector) =>
        JsonResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new { embeddings = new[] { vector } }));

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => send(request, cancellationToken);
    }
}
