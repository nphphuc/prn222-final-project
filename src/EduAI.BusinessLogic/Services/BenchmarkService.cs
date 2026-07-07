using EduAI.Model;
using EduAI.Model.DTOs;
using EduAI.Model.Enums;
using EduAI.BusinessLogic.IService;
using Microsoft.EntityFrameworkCore;

namespace EduAI.BusinessLogic.Services;

public class BenchmarkService : IBenchmarkService
{
    private readonly MetricsCollector _collector;
    private readonly AppDbContext _db;

    public BenchmarkService(MetricsCollector collector, AppDbContext db)
    {
        _collector = collector;
        _db = db;
    }

    public async Task<MetricsDashboardDto> GetDashboardAsync()
    {
        var snapshot = _collector.GetSnapshot();

        var dto = new MetricsDashboardDto
        {
            // AI response
            AiRequestCount = snapshot.AiCount,
            AiMinResponseMs = Math.Round(snapshot.AiMinMs, 1),
            AiMaxResponseMs = Math.Round(snapshot.AiMaxMs, 1),
            AiErrorCount = snapshot.AiErrorCount,

            // Embedding
            EmbeddingCount = snapshot.EmbedCount,
            EmbeddingMinResponseMs = Math.Round(snapshot.EmbedMinMs, 1),
            EmbeddingMaxResponseMs = Math.Round(snapshot.EmbedMaxMs, 1),

            // Index
            IndexCount = snapshot.IndexCount,
            IndexMinDurationMs = Math.Round(snapshot.IndexMinMs, 1),
            IndexMaxDurationMs = Math.Round(snapshot.IndexMaxMs, 1),

            // Latest ops
            LatestOperations = snapshot.LatestOperations
        };

        // Compute averages
        dto.AiAvgResponseMs = snapshot.AiCount > 0
            ? Math.Round(snapshot.AiTotalMs / snapshot.AiCount, 1)
            : 0;
        dto.EmbeddingAvgResponseMs = snapshot.EmbedCount > 0
            ? Math.Round(snapshot.EmbedTotalMs / snapshot.EmbedCount, 1)
            : 0;
        dto.IndexAvgDurationMs = snapshot.IndexCount > 0
            ? Math.Round(snapshot.IndexTotalMs / snapshot.IndexCount, 1)
            : 0;

        // DB-level counts (lightweight aggregates)
        var docs = _db.Documents.AsNoTracking();
        dto.TotalDocuments = await docs.CountAsync();
        dto.TotalIndexedDocuments = await docs.CountAsync(d => d.IndexStatus == DocumentIndexStatus.Indexed);

        var chunks = _db.DocumentChunks.AsNoTracking();
        dto.TotalChunks = await chunks.CountAsync();

        var embeddings = _db.DocumentEmbeddings.AsNoTracking();
        dto.TotalEmbeddings = await embeddings.CountAsync();

        return dto;
    }
}
