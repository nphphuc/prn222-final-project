using EduAI.BusinessLogic.IService;
using EduAI.Model.DTOs;
using EduAI.Model.Enums;
using EduAI.Model.ViewModels;
using EduAI.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EduAI.Web.Pages.Configuration;

[Authorize(Policy = "AdminOnly")]
public class IndexModel : PageModel
{
    private readonly ISystemConfigurationService _configService;

    public IndexModel(ISystemConfigurationService configService)
    {
        _configService = configService;
    }

    [BindProperty]
    public UpdateSystemConfigurationDto Input { get; set; } = new();

    public SystemConfigurationViewModel ViewModel { get; set; } = new();

    public SelectList StrategyOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        var config = await _configService.GetAsync();
        ViewModel.Configuration = config;

        Input.ChunkingStrategy = config.ChunkingStrategy;
        Input.ChunkSize = config.ChunkSize;
        Input.ChunkOverlap = config.ChunkOverlap;
        Input.CitationMinSimilarity = config.CitationMinSimilarity;
        Input.CitationEnabled = config.CitationEnabled;
        Input.ChatTopK = config.ChatTopK;

        StrategyOptions = new SelectList(
            Enum.GetValues<ChunkingStrategy>()
                .Select(s => new { Value = (int)s, Text = GetStrategyLabel(s) }),
            "Value",
            "Text",
            (int)config.ChunkingStrategy);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        StrategyOptions = new SelectList(
            Enum.GetValues<ChunkingStrategy>()
                .Select(s => new { Value = (int)s, Text = GetStrategyLabel(s) }),
            "Value",
            "Text",
            (int)Input.ChunkingStrategy);

        if (!ModelState.IsValid)
        {
            var current = await _configService.GetAsync();
            ViewModel.Configuration = current;
            return Page();
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var result = await _configService.UpdateAsync(
            Input, userId, IpAddressHelper.GetClientIp(HttpContext));

        ViewModel.Configuration = result.Configuration ?? await _configService.GetAsync();

        if (!result.Success)
        {
            ViewModel.ErrorMessage = result.ErrorMessage ?? "Cập nhật thất bại.";
            return Page();
        }

        ViewModel.SuccessMessage = "Cấu hình hệ thống đã được cập nhật thành công.";
        return Page();
    }

    private static string GetStrategyLabel(ChunkingStrategy strategy) => strategy switch
    {
        ChunkingStrategy.Paragraph => "Paragraph (theo đoạn văn)",
        ChunkingStrategy.CharacterCount => "Character count (theo ký tự)",
        ChunkingStrategy.SizeBased => "Size-based (theo kích thước)",
        _ => strategy.ToString()
    };
}
