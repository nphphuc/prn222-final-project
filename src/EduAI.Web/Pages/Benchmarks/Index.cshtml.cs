using EduAI.BusinessLogic.IService;
using EduAI.Model.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EduAI.Web.Pages.Benchmarks;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly IBenchmarkService _benchmarkService;

    public IndexModel(IBenchmarkService benchmarkService)
    {
        _benchmarkService = benchmarkService;
    }

    public MetricsDashboardDto Data { get; set; } = new();

    public async Task OnGetAsync()
    {
        Data = await _benchmarkService.GetDashboardAsync();
    }

    /// <summary>AJAX handler for auto-refresh.</summary>
    public async Task<JsonResult> OnGetMetricsAsync()
    {
        var data = await _benchmarkService.GetDashboardAsync();
        return new JsonResult(data);
    }
}
