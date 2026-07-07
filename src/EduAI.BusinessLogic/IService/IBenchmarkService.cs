using EduAI.Model.DTOs;

namespace EduAI.BusinessLogic.IService;

public interface IBenchmarkService
{
    /// <summary>Get full benchmark/metrics dashboard data.</summary>
    Task<MetricsDashboardDto> GetDashboardAsync();
}
