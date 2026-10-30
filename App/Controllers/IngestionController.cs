using App.Clients;
using Microsoft.AspNetCore.Mvc;

namespace App.Controllers;

[ApiController]
[Route("v1/ingest")]
public sealed class IngestionController(
    IEmbeddingClient embeddingClient,
    ILogger<IngestionController> logger) : ControllerBase
{
    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<IngestionResponse>> Post(
        [FromBody] IngestionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest();
        }

        try
        {
            int? dimensions = null;
            long count = 0;
            await foreach (var chunk in embeddingClient.CreateEmbeddingsAsync(request.Text, cancellationToken))
            {
                if (chunk.Vector.Length == 0
                    || !chunk.Vector.All(float.IsFinite)
                    || (dimensions is not null && dimensions.Value != chunk.Vector.Length))
                {
                    throw new EmbeddingException("The embedding client returned inconsistent vectors.");
                }

                dimensions = chunk.Vector.Length;
                count++;
            }

            if (dimensions is null)
            {
                throw new EmbeddingException("The embedding client returned no vectors.");
            }

            return Ok(new IngestionResponse(dimensions.Value, count));
        }
        catch (EmbeddingInputException)
        {
            return BadRequest();
        }
        catch (EmbeddingException exception)
        {
            logger.LogWarning("Embedding failed: {Reason}", exception.Message);
            return Problem(
                title: exception.IsTimeout ? "Embedding service timed out." : "Embedding service failed.",
                statusCode: exception.IsTimeout
                    ? StatusCodes.Status504GatewayTimeout
                    : StatusCodes.Status502BadGateway);
        }
    }
}

public sealed class IngestionRequest
{
    public string? Text { get; set; }
}

public sealed record IngestionResponse(int EmbeddingSize, long EmbeddingCount);
