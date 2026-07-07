using EduAI.Model.Enums;

namespace EduAI.Model.Entities;

/// <summary>
/// Stores system-wide configuration settings that admin can modify via UI.
/// There is always exactly one row (singleton pattern, Id = 1).
/// </summary>
public class SystemConfiguration : BaseEntity
{
    // ── Chunking settings ──────────────────────────────────────────────
    public ChunkingStrategy ChunkingStrategy { get; set; } = ChunkingStrategy.CharacterCount;

    /// <summary>Character count per chunk (used when strategy is CharacterCount).</summary>
    public int ChunkSize { get; set; } = 800;

    /// <summary>Overlap character count between consecutive chunks.</summary>
    public int ChunkOverlap { get; set; } = 120;

    // ── Citation settings ──────────────────────────────────────────────
    /// <summary>Minimum cosine similarity score (0.0 – 1.0) for a chunk to be cited.</summary>
    public double CitationMinSimilarity { get; set; } = 0.55;

    /// <summary>When set to false, citations are hidden from the student UI.</summary>
    public bool CitationEnabled { get; set; } = true;

    // ── Chat retrieval settings ────────────────────────────────────────
    /// <summary>Number of top relevant chunks to retrieve per question.</summary>
    public int ChatTopK { get; set; } = 5;
}
