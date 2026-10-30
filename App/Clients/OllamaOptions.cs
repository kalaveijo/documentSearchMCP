namespace App.Clients;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://ollama:11434";
    public string Model { get; set; } = "embeddinggemma";
    public int TimeoutSeconds { get; set; } = 100;

    public static bool IsValidBaseUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.AbsolutePath == "/"
        && uri.Query.Length == 0
        && uri.Fragment.Length == 0
        && uri.UserInfo.Length == 0;
}
