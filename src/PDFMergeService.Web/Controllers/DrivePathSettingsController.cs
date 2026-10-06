using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Core.Enums;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Web.Controllers;

[Authorize(Roles = "Admin")]
public class DrivePathSettingsController : Controller
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    private readonly IDrivePathConfigService _drivePathConfigService;
    private readonly IActivityLogService _activityLogService;

    public DrivePathSettingsController(
        IDrivePathConfigService drivePathConfigService,
        IActivityLogService activityLogService)
    {
        _drivePathConfigService = drivePathConfigService;
        _activityLogService = activityLogService;
    }

    [HttpGet("/drive-paths")]
    public async Task<IActionResult> Index() => View(await _drivePathConfigService.GetAsync());

    [HttpPost("/drive-paths")]
    public async Task<IActionResult> Save([FromBody] DrivePathConfig? model)
    {
        if (model == null)
            return BadRequest(new { error = "Geçersiz istek." });

        var config = new DrivePathConfig
        {
            WebPathOptions = Normalize(model.WebPathOptions),
            BulkWebPath = model.BulkWebPath?.Trim() ?? string.Empty,
            BulkUploadRules = NormalizeRules(model.BulkUploadRules)
        };

        var error = Validate(config.WebPathOptions, "Site alt yolu") ?? ValidateRules(config.BulkUploadRules);
        if (error != null)
            return BadRequest(new { error });

        await _drivePathConfigService.SaveAsync(config);

        await _activityLogService.LogAsync(new ActivityLogEntry
        {
            Username = User.Identity?.Name ?? "unknown",
            EventType = ActivityEventType.DrivePathConfigUpdate,
            Detail = $"{config.WebPathOptions.Count} site alt yolu, {config.BulkUploadRules.Count} toplu yükleme kuralı, " +
                     $"toplu webPath: {config.BulkWebPath}",
            Success = true
        });

        return Ok(new { message = "Drive yolları kaydedildi." });
    }

    // Tamamen boş bırakılan satırlar (eklenip doldurulmamış) sessizce atlanır.
    private static List<WebPathOption> Normalize(List<WebPathOption>? options) =>
        (options ?? new())
            .Select(o => new WebPathOption { Label = o.Label?.Trim() ?? string.Empty, Value = o.Value?.Trim() ?? string.Empty })
            .Where(o => o.Label.Length > 0 || o.Value.Length > 0)
            .ToList();

    private static List<BulkUploadRule> NormalizeRules(List<BulkUploadRule>? rules) =>
        (rules ?? new())
            .Select(r => new BulkUploadRule
            {
                FileSuffix = r.FileSuffix?.Trim() ?? string.Empty,
                FolderSuffix = r.FolderSuffix?.Trim() ?? string.Empty,
                RootPath = r.RootPath?.Trim().TrimEnd('/') ?? string.Empty,
                PeriodFolderFormat = r.PeriodFolderFormat?.Trim() ?? string.Empty
            })
            .Where(r => r.FileSuffix.Length > 0 || r.FolderSuffix.Length > 0 || r.RootPath.Length > 0 || r.PeriodFolderFormat.Length > 0)
            .ToList();

    private static string? ValidateRules(List<BulkUploadRule> rules)
    {
        foreach (var rule in rules)
        {
            var name = rule.FileSuffix.Length > 0 ? $"\"{rule.FileSuffix}\" kuralında" : "Toplu yükleme kurallarında";

            if (rule.FileSuffix.Length == 0)
                return "Toplu yükleme kurallarında dosya adı eki boş bir satır var.";
            if (rule.RootPath.Length == 0)
                return $"{name} ana klasör boş.";
            if (!rule.PeriodFolderFormat.Contains("{YIL}") || !rule.PeriodFolderFormat.Contains("{CEYREK}"))
                return $"{name} dönem klasörü formatı {{YIL}} ve {{CEYREK}} ifadelerini içermeli.";
        }

        var duplicate = rules
            .GroupBy(r => r.FileSuffix, StringComparer.Create(TurkishCulture, ignoreCase: true))
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
            return $"Toplu yükleme kurallarında \"{duplicate.Key}\" eki birden fazla kez girilmiş.";

        return null;
    }

    private static string? Validate(List<WebPathOption> options, string groupName)
    {
        if (options.Any(o => o.Label.Length == 0 || o.Value.Length == 0))
            return $"{groupName} listesinde adı veya değeri boş satır var.";

        var duplicate = options
            .GroupBy(o => o.Value, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
            return $"{groupName} listesinde \"{duplicate.Key}\" değeri birden fazla kez girilmiş.";

        return null;
    }
}
