using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Core.Enums;
using PDFMergeService.Core.Helpers;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Web.ViewModels.DriveTransfer;

namespace PDFMergeService.Web.Controllers;

public class DriveTransferController : Controller
{
    private readonly IDriveTransferService _driveTransferService;
    private readonly IDrivePathConfigService _drivePathConfigService;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<DriveTransferController> _logger;

    public DriveTransferController(
        IDriveTransferService driveTransferService,
        IDrivePathConfigService drivePathConfigService,
        IActivityLogService activityLogService,
        ILogger<DriveTransferController> logger)
    {
        _driveTransferService = driveTransferService;
        _drivePathConfigService = drivePathConfigService;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    [HttpGet("/drive-transfer")]
    public async Task<IActionResult> Index()
    {
        var config = await _drivePathConfigService.GetAsync();
        return View(new DriveTransferIndexViewModel
        {
            WebPathOptions = config.WebPathOptions,
            BulkUploadRules = config.BulkUploadRules,
            BulkPathOverrides = config.BulkPathOverrides
        });
    }

    [HttpPost("/drive-transfer/upload")]
    public async Task<IActionResult> Upload([FromForm] DriveTransferUploadViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
            return BadRequest(new { error = "Lütfen bir PDF dosyası seçin." });

        if (string.IsNullOrWhiteSpace(model.WebPath) || string.IsNullOrWhiteSpace(model.Path))
            return BadRequest(new { error = "Hedef yol (site alt yolu / klasör yolu) boş olamaz." });

        var fileName = string.IsNullOrWhiteSpace(model.FileName) ? model.File.FileName : model.FileName.Trim();

        var (success, message) = await UploadOneAsync(
            model.File, model.WebPath.Trim(), model.Path.Trim(), fileName, model.ExtraParams, model.IsMergedVersion);

        await _activityLogService.LogAsync(new ActivityLogEntry
        {
            Username = User.Identity?.Name ?? "unknown",
            EventType = ActivityEventType.DriveTransfer,
            Detail = $"{fileName} → {model.WebPath.Trim()}/{model.Path.Trim()}",
            Success = success
        });

        if (!success)
            return StatusCode(502, new { error = message });

        return Ok(new { message });
    }

    [HttpPost("/drive-transfer/upload-bulk")]
    public async Task<IActionResult> UploadBulk(
        List<IFormFile> files,
        [FromForm] List<string> paths,
        [FromForm] List<bool> isMergedVersions)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "Lütfen en az bir PDF dosyası seçin." });

        var config = await _drivePathConfigService.GetAsync();
        if (string.IsNullOrWhiteSpace(config.BulkWebPath))
            return StatusCode(500, new { error = "Toplu yükleme site alt yolu (BulkWebPath) tanımlı değil. Drive Yolları sayfasından ayarlayın." });

        var webPath = config.BulkWebPath.Trim();
        var results = new List<object>();
        var allSuccess = true;

        for (var i = 0; i < files.Count; i++)
        {
            var file = files[i];
            var path = i < paths.Count ? paths[i] : string.Empty;
            var isMerged = i < isMergedVersions.Count && isMergedVersions[i];
            var fileName = file?.FileName ?? "(bilinmiyor)";

            if (file == null || file.Length == 0)
            {
                results.Add(new { fileName, path, success = false, message = "Dosya boş veya okunamadı." });
                allSuccess = false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                results.Add(new { fileName, path, success = false, message = "Hedef klasör (path) boş, lütfen elle girin." });
                allSuccess = false;
                continue;
            }

            var (success, message) = await UploadOneAsync(
                file, webPath, path.Trim(), file.FileName, extraParamsRaw: null, isMerged);

            results.Add(new { fileName, path, success, message });
            allSuccess = allSuccess && success;
        }

        await _activityLogService.LogAsync(new ActivityLogEntry
        {
            Username = User.Identity?.Name ?? "unknown",
            EventType = ActivityEventType.DriveTransfer,
            Detail = $"{files.Count} dosya → {webPath} (toplu)",
            Success = allSuccess
        });

        return Ok(new { results });
    }

    private async Task<(bool Success, string Message)> UploadOneAsync(
        IFormFile file, string webPath, string path, string fileName, string? extraParamsRaw, bool isMergedVersion)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);

        var title = Path.GetFileNameWithoutExtension(fileName);

        var request = new DriveUploadRequest
        {
            UserId = ResolveUploaderUserId(),
            WebPath = webPath,
            Path = path,
            FileName = fileName,
            ExtraParams = BuildExtraParams(extraParamsRaw, title, isMergedVersion),
            FileBytes = ms.ToArray()
        };

        try
        {
            var result = await _driveTransferService.UploadAsync(request);
            return (result.Success, result.Message ?? (result.Success
                ? "Dosya SharePoint'e aktarıldı."
                : "SharePoint aktarımı başarısız."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drive aktarım hatası: {FileName}", fileName);
            return (false, "Aktarım sırasında beklenmeyen bir hata oluştu.");
        }
    }

    // SharePoint'te dosyayı kimin yüklediği görünsün diye giriş yapan kullanıcının sicilinden türetilir.
    // Sicil beklenen biçimde değilse null döner ve servis appsettings'teki sabit UserId'yi kullanır.
    private int? ResolveUploaderUserId()
    {
        var username = User.Identity?.Name;
        if (SicilUserIdConverter.TryConvert(username, out var userId))
            return userId;

        _logger.LogWarning("Sicil SharePoint kullanıcı id'sine çevrilemedi, varsayılan UserId kullanılacak: {Username}", username);
        return null;
    }

    private static string[] BuildExtraParams(string? raw, string title, bool isMergedVersion)
    {
        var userParams = string.IsNullOrWhiteSpace(raw)
            ? Enumerable.Empty<string>()
            : raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                 .Where(line => !line.StartsWith("Title|", StringComparison.OrdinalIgnoreCase)
                             && !line.StartsWith("ACCIsMergedVersion|", StringComparison.OrdinalIgnoreCase));

        var autoParams = new[]
        {
            $"Title|{title}|",
            $"ACCIsMergedVersion|{(isMergedVersion ? "true" : "false")}|"
        };

        return userParams.Concat(autoParams).ToArray();
    }
}
