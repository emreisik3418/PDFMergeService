using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IActivityLogService
{
    Task LogAsync(ActivityLogEntry entry);

    Task<IReadOnlyList<ActivityLogEntry>> GetLogsAsync(DateTime? from, DateTime? to, string? username);
}
