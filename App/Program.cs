using App.Clients;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOptions<OllamaOptions>()
    .Bind(builder.Configuration.GetSection(OllamaOptions.SectionName))
    .Validate(options => OllamaOptions.IsValidBaseUrl(options.BaseUrl),
        "Ollama:BaseUrl must be an HTTP or HTTPS origin.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Model),
        "Ollama:Model must not be empty.")
    .Validate(options => options.TimeoutSeconds is > 0 and <= 2_147_483,
        "Ollama:TimeoutSeconds must be between 1 and 2147483.")
    .ValidateOnStart();
builder.Services.AddHttpClient<IEmbeddingClient, OllamaEmbeddingClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

var app = builder.Build();

app.MapControllers();

app.Run();

public partial class Program;
