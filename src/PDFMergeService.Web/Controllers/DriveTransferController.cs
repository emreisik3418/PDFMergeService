using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;
using PDFMergeService.Web.ViewModels.DriveTransfer;

namespace PDFMergeService.Web.Controllers;

public class DriveTransferController : Controller
{
    private readonly IDriveTransferService _driveTransferService;
    private readonly SharePointSettings _sharePointSettings;
    private readonly ILogger<DriveTransferController> _logger;

    public DriveTransferController(
        IDriveTransferService driveTransferService,
        IOptions<SharePointSettings> sharePointSettings,
        ILogger<DriveTransferController> logger)
    {
        _driveTransferService = driveTransferService;
        _sharePointSettings = sharePointSettings.Value;
        _logger = logger;
    }

    [HttpGet("/drive-transfer")]
    public IActionResult Index() => View(new DriveTransferIndexViewModel
    {
        WebPathOptions = _sharePointSettings.WebPathOptions,
        BulkRootPathOptions = _sharePointSettings.BulkRootPathOptions
    });

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

        if (!success)
            return StatusCode(502, new { error = message });

        return Ok(new { message });
    }

    [HttpPost("/drive-transfer/upload-bulk")]
    public async Task<IActionResult> UploadBulk([FromForm] DriveTransferBulkUploadViewModel model)
    {
        if (model.Items == null || model.Items.Count == 0)
            return BadRequest(new { error = "Lütfen en az bir PDF dosyası seçin." });

        if (string.IsNullOrWhiteSpace(_sharePointSettings.BulkWebPath))
            return StatusCode(500, new { error = "appsettings içinde SharePointSettings:BulkWebPath tanımlı değil." });

        var webPath = _sharePointSettings.BulkWebPath.Trim();
        var results = new List<object>();

        foreach (var item in model.Items)
        {
            var fileName = item.File?.FileName ?? "(bilinmiyor)";

            if (item.File == null || item.File.Length == 0)
            {
                results.Add(new { fileName, path = item.Path, success = false, message = "Dosya boş veya okunamadı." });
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Path))
            {
                results.Add(new { fileName, path = item.Path, success = false, message = "Hedef klasör (path) boş, lütfen elle girin." });
                continue;
            }

            var (success, message) = await UploadOneAsync(
                item.File, webPath, item.Path.Trim(), item.File.FileName, extraParamsRaw: null, item.IsMergedVersion);

            results.Add(new { fileName, path = item.Path, success, message });
        }

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
