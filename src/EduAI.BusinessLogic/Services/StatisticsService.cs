using EduAI.Model;
using EduAI.Model.Constants;
using EduAI.Model.DTOs;
using EduAI.Model.Entities;
using EduAI.Model.Enums;
using EduAI.BusinessLogic.IService;
using Microsoft.EntityFrameworkCore;

namespace EduAI.BusinessLogic.Services;

public class StatisticsService : IStatisticsService
{
    private readonly AppDbContext _db;

    public StatisticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<StatisticsDashboardDto> GetDashboardAsync()
    {
        var dto = new StatisticsDashboardDto();

        await LoadUserStatsAsync(dto);
        await LoadDocumentStatsAsync(dto);
        await LoadChatStatsAsync(dto);
        await LoadAuditStatsAsync(dto);

        return dto;
    }

    // ── User statistics ──────────────────────────────────────────
    // Aggregates directly from AspNetUsers + AspNetUserRoles in a single pass.

    private async Task LoadUserStatsAsync(StatisticsDashboardDto dto)
    {
        var allUsers = _db.Users.AsNoTracking();

        // Basic counts
        dto.TotalUsers = await allUsers.CountAsync();
        dto.ActiveUsers = await allUsers.CountAsync(u => u.IsActive);

        // Role counts via LINQ join with Identity tables (no raw SQL)
        var userRoles = _db.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().AsNoTracking();
        var roles = _db.Roles.AsNoTracking();

        var roleCounts = await
            (from u in allUsers
             join ur in userRoles on u.Id equals ur.UserId
             join r in roles on ur.RoleId equals r.Id
             group r by r.Name into g
             select new { RoleName = g.Key, Count = g.Count() })
            .ToListAsync();

        dto.TotalAdmins = roleCounts.FirstOrDefault(r => r.RoleName == Roles.Admin)?.Count ?? 0;
        dto.TotalTeachers = roleCounts.FirstOrDefault(r => r.RoleName == Roles.Teacher)?.Count ?? 0;
        dto.TotalStudents = roleCounts.FirstOrDefault(r => r.RoleName == Roles.Student)?.Count ?? 0;

        // Daily registrations (last 30 days) — aggregate at DB level
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        dto.DailyUserRegistrations = await allUsers
            .Where(u => u.CreatedAt >= thirtyDaysAgo)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new TimeSeriesPoint { Date = g.Key, Count = g.Count() })
            .OrderBy(p => p.Date)
            .ToListAsync();
    }

    // ── Document statistics ──────────────────────────────────────
    // All aggregates execute as SQL COUNT / SUM / GROUP BY.

    private async Task LoadDocumentStatsAsync(StatisticsDashboardDto dto)
    {
        var docs = _db.Documents.AsNoTracking();

        dto.TotalDocuments = await docs.CountAsync();
        dto.DocumentsIndexed = await docs.CountAsync(d => d.IndexStatus == DocumentIndexStatus.Indexed);
        dto.DocumentsPending = await docs.CountAsync(d => d.IndexStatus == DocumentIndexStatus.Pending);
        dto.TotalFileSizeBytes = await docs.SumAsync(d => (long?)d.FileSizeBytes) ?? 0;

        // By category (enum GroupBy at DB level, then map label in C#)
        var categoryData = await docs
            .GroupBy(d => d.Category)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
        dto.DocumentsByCategory = categoryData
            .Select(x => new BreakdownItem { Label = x.Category.ToVietnameseLabel(), Count = x.Count })
            .ToList();

        // By index status
        var statusData = await docs
            .GroupBy(d => d.IndexStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();
        dto.DocumentsByStatus = statusData
            .Select(x => new BreakdownItem { Label = x.Status.ToVietnameseLabel(), Count = x.Count })
            .ToList();

        // Daily uploads (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        dto.DailyDocumentUploads = await docs
            .Where(d => d.CreatedAt >= thirtyDaysAgo)
            .GroupBy(d => d.CreatedAt.Date)
            .Select(g => new TimeSeriesPoint { Date = g.Key, Count = g.Count() })
            .OrderBy(p => p.Date)
            .ToListAsync();
    }

    // ── Chat statistics ──────────────────────────────────────────

    private async Task LoadChatStatsAsync(StatisticsDashboardDto dto)
    {
        // Use two lightweight count queries instead of loading all rows.
        dto.TotalChatSessions = await _db.ChatSessions.AsNoTracking().CountAsync();
        dto.TotalChatMessages = await _db.ChatMessages.AsNoTracking().CountAsync();

        // Messages by role (raw SQL since the label mapping is a C# expression)
        var roleCounts = await _db.ChatMessages.AsNoTracking()
            .GroupBy(m => m.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync();

        dto.MessagesByRole = roleCounts
            .Select(x => new BreakdownItem
            {
                Label = x.Role == "user" ? "Người dùng" : "Trợ lý AI",
                Count = x.Count
            })
            .ToList();

        // Daily messages (last 30 days)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        dto.DailyChatMessages = await _db.ChatMessages.AsNoTracking()
            .Where(m => m.CreatedAt >= thirtyDaysAgo)
            .GroupBy(m => m.CreatedAt.Date)
            .Select(g => new TimeSeriesPoint { Date = g.Key, Count = g.Count() })
            .OrderBy(p => p.Date)
            .ToListAsync();
    }

    // ── Audit / AI statistics ────────────────────────────────────
    // Previously loaded ALL audit logs into memory — now aggregates at DB level.

    private async Task LoadAuditStatsAsync(StatisticsDashboardDto dto)
    {
        var audits = _db.AuditLogs.AsNoTracking();

        // Total AI request count (single COUNT query)
        dto.TotalAiRequests = await audits.CountAsync(l => l.Action == AuditActions.AiRequest);

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // Daily AI requests in last 30 days
        dto.DailyAiRequests = await audits
            .Where(l => l.Action == AuditActions.AiRequest && l.Timestamp >= thirtyDaysAgo)
            .GroupBy(l => l.Timestamp.Date)
            .Select(g => new TimeSeriesPoint { Date = g.Key, Count = g.Count() })
            .OrderBy(p => p.Date)
            .ToListAsync();

        // Top 10 actions (excluding AiRequest) in last 30 days
        dto.TopActions = await audits
            .Where(l => l.Action != AuditActions.AiRequest && l.Timestamp >= thirtyDaysAgo)
            .GroupBy(l => l.Action)
            .Select(g => new BreakdownItem { Label = g.Key, Count = g.Count() })
            .OrderByDescending(b => b.Count)
            .Take(10)
            .ToListAsync();
    }


}
