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
        WebPathOptions = _sharePointSettings.WebPathOptions
    });

    [HttpPost("/drive-transfer/upload")]
    public async Task<IActionResult> Upload([FromForm] DriveTransferUploadViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
            return BadRequest(new { error = "Lütfen bir PDF dosyası seçin." });

        if (string.IsNullOrWhiteSpace(model.WebPath) || string.IsNullOrWhiteSpace(model.Path))
            return BadRequest(new { error = "Hedef yol (site alt yolu / klasör yolu) boş olamaz." });

        using var ms = new MemoryStream();
        await model.File.CopyToAsync(ms);

        var fileName = string.IsNullOrWhiteSpace(model.FileName) ? model.File.FileName : model.FileName.Trim();
        var title = Path.GetFileNameWithoutExtension(fileName);

        var request = new DriveUploadRequest
        {
            WebPath = model.WebPath.Trim(),
            Path = model.Path.Trim(),
            FileName = fileName,
            ExtraParams = BuildExtraParams(model.ExtraParams, title, model.IsMergedVersion),
            FileBytes = ms.ToArray()
        };

        try
        {
            var result = await _driveTransferService.UploadAsync(request);
            if (!result.Success)
                return StatusCode(502, new { error = result.Message ?? "SharePoint aktarımı başarısız." });

            return Ok(new { message = result.Message ?? "Dosya SharePoint'e aktarıldı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drive aktarım hatası: {FileName}", model.File.FileName);
            return StatusCode(500, new { error = "Aktarım sırasında beklenmeyen bir hata oluştu." });
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
