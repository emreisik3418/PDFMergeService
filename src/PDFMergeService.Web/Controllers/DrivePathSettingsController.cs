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
            BulkRootPathOptions = Normalize(model.BulkRootPathOptions),
            BulkWebPath = model.BulkWebPath?.Trim() ?? string.Empty,
            RegionSuffixes = (model.RegionSuffixes ?? new())
                .Select(s => s?.Trim() ?? string.Empty)
                .Where(s => s.Length > 0)
                .ToList()
        };

        var error = Validate(config.WebPathOptions, "Site alt yolu")
                 ?? Validate(config.BulkRootPathOptions, "Klasör kökü");

        var duplicateSuffix = config.RegionSuffixes
            .GroupBy(s => s, StringComparer.Create(TurkishCulture, ignoreCase: true))
            .FirstOrDefault(g => g.Count() > 1);
        if (error == null && duplicateSuffix != null)
            error = $"Bölge/şube ekleri listesinde \"{duplicateSuffix.Key}\" birden fazla kez girilmiş.";
        if (error != null)
            return BadRequest(new { error });

        await _drivePathConfigService.SaveAsync(config);

        await _activityLogService.LogAsync(new ActivityLogEntry
        {
            Username = User.Identity?.Name ?? "unknown",
            EventType = ActivityEventType.DrivePathConfigUpdate,
            Detail = $"{config.WebPathOptions.Count} site alt yolu, {config.BulkRootPathOptions.Count} klasör kökü, " +
                     $"{config.RegionSuffixes.Count} bölge/şube eki, toplu webPath: {config.BulkWebPath}",
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
