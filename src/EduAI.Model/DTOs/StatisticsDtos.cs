namespace EduAI.Model.DTOs;

public class StatisticsDashboardDto
{
    // ── Summary cards ────────────────────────────────────────────
    public int TotalUsers { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveUsers { get; set; }

    public int TotalDocuments { get; set; }
    public int DocumentsIndexed { get; set; }
    public int DocumentsPending { get; set; }
    public long TotalFileSizeBytes { get; set; }

    public int TotalChatSessions { get; set; }
    public int TotalChatMessages { get; set; }
    public int TotalAiRequests { get; set; }

    // ── Time-series (for charts) ─────────────────────────────────
    public List<TimeSeriesPoint> DailyUserRegistrations { get; set; } = [];
    public List<TimeSeriesPoint> DailyDocumentUploads { get; set; } = [];
    public List<TimeSeriesPoint> DailyAiRequests { get; set; } = [];
    public List<TimeSeriesPoint> DailyChatMessages { get; set; } = [];

    // ── Breakdown ────────────────────────────────────────────────
    public List<BreakdownItem> DocumentsByCategory { get; set; } = [];
    public List<BreakdownItem> DocumentsByStatus { get; set; } = [];
    public List<BreakdownItem> MessagesByRole { get; set; } = [];
    public List<BreakdownItem> TopActions { get; set; } = [];
}

public class TimeSeriesPoint
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

public class BreakdownItem
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
