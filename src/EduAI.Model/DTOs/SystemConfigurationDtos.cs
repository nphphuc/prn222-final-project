using EduAI.Model.Enums;

namespace EduAI.Model.DTOs;

public class SystemConfigurationDto
{
    public ChunkingStrategy ChunkingStrategy { get; set; }
    public int ChunkSize { get; set; }
    public int ChunkOverlap { get; set; }
    public double CitationMinSimilarity { get; set; }
    public bool CitationEnabled { get; set; }
    public int ChatTopK { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByUserName { get; set; }
}

public class UpdateSystemConfigurationDto
{
    public ChunkingStrategy ChunkingStrategy { get; set; }
    public int ChunkSize { get; set; } = 800;
    public int ChunkOverlap { get; set; } = 120;
    public double CitationMinSimilarity { get; set; } = 0.55;
    public bool CitationEnabled { get; set; } = true;
    public int ChatTopK { get; set; } = 5;
}

public class SystemConfigurationOperationResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public SystemConfigurationDto? Configuration { get; set; }
}
