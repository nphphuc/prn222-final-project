using EduAI.Model.DTOs;

namespace EduAI.BusinessLogic.IService;

public interface ISystemConfigurationService
{
    /// <summary>Get the current system configuration (always returns a valid config).</summary>
    Task<SystemConfigurationDto> GetAsync();

    /// <summary>Update system configuration. Only admins can call this.</summary>
    Task<SystemConfigurationOperationResultDto> UpdateAsync(
        UpdateSystemConfigurationDto dto, string userId, string? ipAddress);
}
