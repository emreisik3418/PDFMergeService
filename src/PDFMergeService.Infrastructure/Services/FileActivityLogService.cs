using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Services;

// Singleton (diğer servislerin aksine): paylaşılan log dosyasına yazımı tek instance'ta
// SemaphoreSlim ile serileştirmek, Scoped kayıttan daha güvenilir.
public class FileActivityLogService : IActivityLogService
{
    private readonly ActivityLogSettings _settings;
    private readonly ILogger<FileActivityLogService> _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public FileActivityLogService(IOptions<ActivityLogSettings> settings, ILogger<FileActivityLogService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task LogAsync(ActivityLogEntry entry)
    {
        var line = JsonSerializer.Serialize(entry);
        var filePath = GetFilePath(entry.Timestamp);

        await _writeLock.WaitAsync();
        try
        {
            Directory.CreateDirectory(_settings.LogFolder);
            await File.AppendAllTextAsync(filePath, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aktivite logu yazılamadı: {EventType} / {Username}", entry.EventType, entry.Username);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<IReadOnlyList<ActivityLogEntry>> GetLogsAsync(DateTime? from, DateTime? to, string? username)
    {
        var entries = new List<ActivityLogEntry>();

        if (!Directory.Exists(_settings.LogFolder))
            return entries;

        foreach (var month in EnumerateMonths(from, to))
        {
            var filePath = GetFilePath(month);
            if (!File.Exists(filePath))
                continue;

            foreach (var line in await File.ReadAllLinesAsync(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                ActivityLogEntry? entry;
                try
                {
                    entry = JsonSerializer.Deserialize<ActivityLogEntry>(line);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (entry == null)
                    continue;

                if (from.HasValue && entry.Timestamp < from.Value)
                    continue;

                if (to.HasValue && entry.Timestamp > to.Value)
                    continue;

                if (!string.IsNullOrWhiteSpace(username) &&
                    !entry.Username.Contains(username, StringComparison.OrdinalIgnoreCase))
                    continue;

                entries.Add(entry);
            }
        }

        return entries.OrderByDescending(e => e.Timestamp).ToList();
    }

    private string GetFilePath(DateTime date) =>
        Path.Combine(_settings.LogFolder, $"activity-{date:yyyyMM}.jsonl");

    private static IEnumerable<DateTime> EnumerateMonths(DateTime? from, DateTime? to)
    {
        var start = new DateTime((from ?? DateTime.Now.AddYears(-1)).Year, (from ?? DateTime.Now.AddYears(-1)).Month, 1);
        var end = new DateTime((to ?? DateTime.Now).Year, (to ?? DateTime.Now).Month, 1);

        for (var month = start; month <= end; month = month.AddMonths(1))
            yield return month;
    }
}
