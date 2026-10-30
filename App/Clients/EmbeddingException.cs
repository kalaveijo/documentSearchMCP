namespace App.Clients;

public sealed class EmbeddingException(string message, bool isTimeout = false) : Exception(message)
{
    public bool IsTimeout { get; } = isTimeout;
}

public sealed class EmbeddingInputException(string message) : Exception(message);
