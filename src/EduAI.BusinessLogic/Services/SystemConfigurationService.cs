using EduAI.BusinessLogic.IService;
using EduAI.Model.Constants;
using EduAI.Model.DTOs;
using EduAI.Model.Entities;
using EduAI.Model.Enums;
using EduAI.Model.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EduAI.BusinessLogic.Services;

public class SystemConfigurationService : ISystemConfigurationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SystemConfigurationService> _logger;

    public SystemConfigurationService(
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ILogger<SystemConfigurationService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<SystemConfigurationDto> GetAsync()
    {
        var config = await EnsureConfigExistsAsync();
        return MapToDto(config, null);
    }

    public async Task<SystemConfigurationOperationResultDto> UpdateAsync(
        UpdateSystemConfigurationDto dto, string userId, string? ipAddress)
    {
        if (dto.ChunkSize < 100 || dto.ChunkSize > 10_000)
        {
            return Fail("Chunk size must be between 100 and 10,000 characters.");
        }

        if (dto.ChunkOverlap < 0 || dto.ChunkOverlap >= dto.ChunkSize)
        {
            return Fail("Chunk overlap must be >= 0 and < chunk size.");
        }

        if (dto.CitationMinSimilarity is < 0.0 or > 1.0)
        {
            return Fail("Citation similarity threshold must be between 0.0 and 1.0.");
        }

        if (dto.ChatTopK < 1 || dto.ChatTopK > 50)
        {
            return Fail("Top-K retrieved chunks must be between 1 and 50.");
        }

        var config = await EnsureConfigExistsAsync();

        config.ChunkingStrategy = dto.ChunkingStrategy;
        config.ChunkSize = dto.ChunkSize;
        config.ChunkOverlap = dto.ChunkOverlap;
        config.CitationMinSimilarity = dto.CitationMinSimilarity;
        config.CitationEnabled = dto.CitationEnabled;
        config.ChatTopK = dto.ChatTopK;
        config.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.SystemConfigurations.Update(config);
        await _unitOfWork.SaveChangesAsync();

        await _auditLogService.LogAsync(new CreateAuditLogDto
        {
            UserId = userId,
            Action = AuditActions.UpdateSystemConfig,
            IpAddress = ipAddress,
            Details =
                $"Updated system config: chunking={dto.ChunkingStrategy}, " +
                $"chunkSize={dto.ChunkSize}, overlap={dto.ChunkOverlap}, " +
                $"citationMinSim={dto.CitationMinSimilarity:F2}, citationEnabled={dto.CitationEnabled}, " +
                $"topK={dto.ChatTopK}"
        });

        _logger.LogInformation("System configuration updated by user {UserId}", userId);

        return new SystemConfigurationOperationResultDto
        {
            Success = true,
            Configuration = MapToDto(config, null)
        };
    }

    private async Task<SystemConfiguration> EnsureConfigExistsAsync()
    {
        // FirstOrDefaultAsync returns a tracked entity (no AsNoTracking).
        var config = await _unitOfWork.SystemConfigurations
            .FirstOrDefaultAsync(c => c.Id > 0);
        if (config != null)
            return config;

        config = new SystemConfiguration
        {
            ChunkingStrategy = ChunkingStrategy.CharacterCount,
            ChunkSize = 800,
            ChunkOverlap = 120,
            CitationMinSimilarity = 0.55,
            CitationEnabled = true,
            ChatTopK = 5,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await _unitOfWork.SystemConfigurations.AddAsync(config);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Race condition: another request inserted the config row first.
            // The entity is still in Added state, so discard the context changes.
            // Just return the existing config — it was created by the other request.
            _logger.LogWarning("Config row already exists (race condition). Reloading...");
            config = await _unitOfWork.SystemConfigurations
                .FirstOrDefaultAsync(c => c.Id > 0);
            if (config != null)
                return config;
            throw;
        }

        _logger.LogInformation("System configuration created with defaults.");
        return config;
    }

    private static SystemConfigurationDto MapToDto(SystemConfiguration config, string? updatedByUserName) => new()
    {
        ChunkingStrategy = config.ChunkingStrategy,
        ChunkSize = config.ChunkSize,
        ChunkOverlap = config.ChunkOverlap,
        CitationMinSimilarity = config.CitationMinSimilarity,
        CitationEnabled = config.CitationEnabled,
        ChatTopK = config.ChatTopK,
        UpdatedAt = config.UpdatedAt,
        UpdatedByUserName = updatedByUserName
    };

    private static SystemConfigurationOperationResultDto Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
