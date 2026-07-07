using EduAI.BusinessLogic.IService;
using EduAI.Model.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduAI.Web.Pages.Statistics;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IStatisticsService _statisticsService;

    public IndexModel(IStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public StatisticsDashboardDto Data { get; set; } = new();

    public async Task OnGetAsync()
    {
        Data = await _statisticsService.GetDashboardAsync();
    }
}
