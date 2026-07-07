using EduAI.Model.DTOs;

namespace EduAI.BusinessLogic.Services;

/// <summary>
/// Thread-safe in-memory collector for benchmark/metrics data.
/// Registered as a singleton so data is shared across all requests.
/// </summary>
public class MetricsCollector
{
    private readonly object _lock = new();

    // ── AI (chat) timing ──────────────────────────────────────
    private long _aiCount;
    private double _aiTotalMs;
    private double _aiMinMs = double.MaxValue;
    private double _aiMaxMs;
    private long _aiErrorCount;

    // ── Embedding timing ──────────────────────────────────────
    private long _embedCount;
    private double _embedTotalMs;
    private double _embedMinMs = double.MaxValue;
    private double _embedMaxMs;

    // ── Index timing ──────────────────────────────────────────
    private long _indexCount;
    private double _indexTotalMs;
    private double _indexMinMs = double.MaxValue;
    private double _indexMaxMs;

    // ── Latest operations (ring buffer, keep last 50) ─────────
    private readonly List<LatestOperationDto> _latestOps = [];
    private const int MaxLatestOps = 50;

    // ── Recording methods ─────────────────────────────────────

    public void RecordAiResponse(double elapsedMs)
    {
        lock (_lock)
        {
            _aiCount++;
            _aiTotalMs += elapsedMs;
            if (elapsedMs < _aiMinMs) _aiMinMs = elapsedMs;
            if (elapsedMs > _aiMaxMs) _aiMaxMs = elapsedMs;
        }
    }

    public void RecordAiError()
    {
        lock (_lock)
        {
            _aiErrorCount++;
        }
    }

    public void RecordEmbedding(double elapsedMs)
    {
        lock (_lock)
        {
            _embedCount++;
            _embedTotalMs += elapsedMs;
            if (elapsedMs < _embedMinMs) _embedMinMs = elapsedMs;
            if (elapsedMs > _embedMaxMs) _embedMaxMs = elapsedMs;
        }
    }

    public void RecordIndex(double elapsedMs)
    {
        lock (_lock)
        {
            _indexCount++;
            _indexTotalMs += elapsedMs;
            if (elapsedMs < _indexMinMs) _indexMinMs = elapsedMs;
            if (elapsedMs > _indexMaxMs) _indexMaxMs = elapsedMs;
        }
    }

    public void AddLatestOperation(LatestOperationDto op)
    {
        lock (_lock)
        {
            _latestOps.Insert(0, op);
            if (_latestOps.Count > MaxLatestOps)
                _latestOps.RemoveRange(MaxLatestOps, _latestOps.Count - MaxLatestOps);
        }
    }

    // ── Snapshot ──────────────────────────────────────────────

    public MetricsDto GetSnapshot()
    {
        lock (_lock)
        {
            return new MetricsDto
            {
                AiCount = _aiCount,
                AiTotalMs = _aiTotalMs,
                AiMinMs = _aiMinMs == double.MaxValue ? 0 : _aiMinMs,
                AiMaxMs = _aiMaxMs,
                AiErrorCount = _aiErrorCount,

                EmbedCount = _embedCount,
                EmbedTotalMs = _embedTotalMs,
                EmbedMinMs = _embedMinMs == double.MaxValue ? 0 : _embedMinMs,
                EmbedMaxMs = _embedMaxMs,

                IndexCount = _indexCount,
                IndexTotalMs = _indexTotalMs,
                IndexMinMs = _indexMinMs == double.MaxValue ? 0 : _indexMinMs,
                IndexMaxMs = _indexMaxMs,

                LatestOperations = _latestOps.ToList()
            };
        }
    }
}

/// <summary>Raw snapshot from the collector (used internally to compute averages).</summary>
public class MetricsDto
{
    public long AiCount { get; set; }
    public double AiTotalMs { get; set; }
    public double AiMinMs { get; set; }
    public double AiMaxMs { get; set; }
    public long AiErrorCount { get; set; }

    public long EmbedCount { get; set; }
    public double EmbedTotalMs { get; set; }
    public double EmbedMinMs { get; set; }
    public double EmbedMaxMs { get; set; }

    public long IndexCount { get; set; }
    public double IndexTotalMs { get; set; }
    public double IndexMinMs { get; set; }
    public double IndexMaxMs { get; set; }

    public List<LatestOperationDto> LatestOperations { get; set; } = [];
}
