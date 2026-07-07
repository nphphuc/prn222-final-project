namespace EduAI.Model.DTOs;

/// <summary>Snapshot of all benchmark/metrics data for the dashboard.</summary>
public class MetricsDashboardDto
{
    // ── AI response timing ────────────────────────────────────
    public long AiRequestCount { get; set; }
    public double AiAvgResponseMs { get; set; }
    public double AiMinResponseMs { get; set; }
    public double AiMaxResponseMs { get; set; }
    public long AiErrorCount { get; set; }

    // ── Embedding timing ──────────────────────────────────────
    public long EmbeddingCount { get; set; }
    public double EmbeddingAvgResponseMs { get; set; }
    public double EmbeddingMinResponseMs { get; set; }
    public double EmbeddingMaxResponseMs { get; set; }

    // ── Document indexing timing ──────────────────────────────
    public long IndexCount { get; set; }
    public double IndexAvgDurationMs { get; set; }
    public double IndexMinDurationMs { get; set; }
    public double IndexMaxDurationMs { get; set; }

    // ── Chunks / Embeddings totals (from DB) ──────────────────
    public long TotalChunks { get; set; }
    public long TotalEmbeddings { get; set; }
    public long TotalDocuments { get; set; }
    public long TotalIndexedDocuments { get; set; }

    // ── Latest Operations (for realtime feed) ─────────────────
    public List<LatestOperationDto> LatestOperations { get; set; } = [];
}

public class LatestOperationDto
{
    public string Operation { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsError { get; set; }
}
