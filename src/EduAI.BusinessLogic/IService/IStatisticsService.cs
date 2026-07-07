using EduAI.Model.DTOs;

namespace EduAI.BusinessLogic.IService;

public interface IStatisticsService
{
    /// <summary>Get full statistics dashboard data (admin only).</summary>
    Task<StatisticsDashboardDto> GetDashboardAsync();
}
