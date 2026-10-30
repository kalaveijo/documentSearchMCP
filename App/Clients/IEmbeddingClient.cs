namespace App.Clients;

public interface IEmbeddingClient
{
    IAsyncEnumerable<EmbeddingChunk> CreateEmbeddingsAsync(
        string text,
        CancellationToken cancellationToken = default);
}

public sealed record EmbeddingChunk(string Text, float[] Vector);
