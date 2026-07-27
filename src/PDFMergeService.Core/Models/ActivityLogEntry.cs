using PDFMergeService.Core.Enums;

namespace PDFMergeService.Core.Models;

public class ActivityLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Username { get; set; } = string.Empty;
    public ActivityEventType EventType { get; set; }
    public string Detail { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
}
