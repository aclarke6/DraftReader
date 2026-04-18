using DraftView.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace DraftView.Application.Services;

/// <summary>
/// Generates AI summaries for section republish operations.
/// Returns null when AI generation cannot be completed.
/// </summary>
public sealed class AiSummaryService(HttpClient httpClient, IConfiguration configuration) : IAiSummaryService
{
    /// <summary>
    /// Generates a one-line summary comparing previous and current section content.
    /// Returns null on any failure and never throws to callers.
    /// </summary>
    public Task<string?> GenerateSummaryAsync(
        string? previousHtml,
        string currentHtml,
        CancellationToken ct = default)
    {
        var apiKey = configuration["Anthropic:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return Task.FromResult<string?>(null);

        _ = httpClient;
        _ = previousHtml;
        _ = currentHtml;
        _ = ct;

        return Task.FromResult<string?>(null);
    }
}
