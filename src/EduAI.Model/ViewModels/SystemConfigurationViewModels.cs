using EduAI.Model.DTOs;

namespace EduAI.Model.ViewModels;

public class SystemConfigurationViewModel
{
    public SystemConfigurationDto Configuration { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
}
