using System.Buffers;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace App.Clients;

public sealed class OllamaEmbeddingClient(HttpClient httpClient, IOptions<OllamaOptions> options)
    : IEmbeddingClient
{
    private const int TargetChunkScalars = 2048;
    private const string ContextLengthError = "input exceeds maximum context length";
    private readonly string model = options.Value.Model;

    public async IAsyncEnumerable<EmbeddingChunk> CreateEmbeddingsAsync(
        string text,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new EmbeddingInputException("Text must not be empty or whitespace.");
        }

        int? dimensions = null;
        foreach (var initialChunk in SplitText(text))
        {
            var pending = new Stack<string>();
            pending.Push(initialChunk);

            while (pending.TryPop(out var chunk))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var vector = await EmbedChunkAsync(chunk, cancellationToken);
                if (vector is null)
                {
                    var splitIndex = GetSplitIndex(chunk);
                    if (splitIndex == 0)
                    {
                        throw new EmbeddingException("A text chunk exceeds the model context and cannot be split.");
                    }

                    pending.Push(chunk[splitIndex..]);
                    pending.Push(chunk[..splitIndex]);
                    continue;
                }

                if (dimensions is not null && dimensions.Value != vector.Length)
                {
                    throw new EmbeddingException("Ollama returned inconsistent embedding dimensions.");
                }

                dimensions = vector.Length;
                yield return new EmbeddingChunk(chunk, vector);
            }
        }
    }

    private async Task<float[]?> EmbedChunkAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "api/embed",
                new EmbedRequest(model, text, false),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var error = await response.Content.ReadFromJsonAsync<EmbedError>(cancellationToken);
                if (error?.Error == ContextLengthError)
                {
                    return null;
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new EmbeddingException($"Ollama returned HTTP {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken);
            if (result?.Embeddings is not { Length: 1 }
                || result.Embeddings[0] is not { Length: > 0 } vector
                || !vector.All(float.IsFinite))
            {
                throw new EmbeddingException("Ollama returned an invalid embedding response.");
            }

            return vector;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new EmbeddingException("The Ollama request timed out.", isTimeout: true);
        }
        catch (HttpRequestException)
        {
            throw new EmbeddingException("The Ollama request failed.");
        }
        catch (IOException)
        {
            throw new EmbeddingException("The Ollama response could not be read.");
        }
        catch (JsonException)
        {
            throw new EmbeddingException("Ollama returned an invalid JSON response.");
        }
    }

    private static IEnumerable<string> SplitText(string text)
    {
        var start = 0;
        while (start < text.Length)
        {
            var end = start;
            var whitespaceEnd = start;
            var scalarCount = 0;

            while (end < text.Length && scalarCount < TargetChunkScalars)
            {
                var status = Rune.DecodeFromUtf16(text.AsSpan(end), out var rune, out var consumed);
                if (status != OperationStatus.Done)
                {
                    throw new EmbeddingInputException("Text contains an invalid Unicode sequence.");
                }

                end += consumed;
                scalarCount++;
                if (Rune.IsWhiteSpace(rune))
                {
                    whitespaceEnd = end;
                }
            }

            if (end < text.Length && whitespaceEnd > start)
            {
                end = whitespaceEnd;
            }

            yield return text[start..end];
            start = end;
        }
    }

    private static int GetSplitIndex(string text)
    {
        var scalarCount = 0;
        var index = 0;
        while (index < text.Length)
        {
            Rune.DecodeFromUtf16(text.AsSpan(index), out _, out var consumed);
            index += consumed;
            scalarCount++;
        }

        if (scalarCount < 2)
        {
            return 0;
        }

        index = 0;
        for (var i = 0; i < scalarCount / 2; i++)
        {
            Rune.DecodeFromUtf16(text.AsSpan(index), out _, out var consumed);
            index += consumed;
        }

        return index;
    }

    private sealed record EmbedRequest(string Model, string Input, bool Truncate);
    private sealed record EmbedError(string? Error);
    private sealed record EmbedResponse(float[]?[]? Embeddings);
}
