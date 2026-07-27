using PDFMergeService.Core.Models;

namespace PDFMergeService.Web.ViewModels.ActivityLog;

public class ActivityLogViewModel
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Username { get; set; }
    public List<ActivityLogEntry> Entries { get; set; } = new();
}
