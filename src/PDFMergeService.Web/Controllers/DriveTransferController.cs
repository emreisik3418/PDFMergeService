using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Web.ViewModels.DriveTransfer;

namespace PDFMergeService.Web.Controllers;

public class DriveTransferController : Controller
{
    private readonly IDriveTransferService _driveTransferService;
    private readonly ILogger<DriveTransferController> _logger;

    public DriveTransferController(IDriveTransferService driveTransferService, ILogger<DriveTransferController> logger)
    {
        _driveTransferService = driveTransferService;
        _logger = logger;
    }

    [HttpGet("/drive-transfer")]
    public IActionResult Index() => View();

    [HttpPost("/drive-transfer/upload")]
    public async Task<IActionResult> Upload([FromForm] DriveTransferUploadViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
            return BadRequest(new { error = "Lütfen bir PDF dosyası seçin." });

        if (string.IsNullOrWhiteSpace(model.WebPath) || string.IsNullOrWhiteSpace(model.Path))
            return BadRequest(new { error = "Hedef yol (site alt yolu / klasör yolu) boş olamaz." });

        using var ms = new MemoryStream();
        await model.File.CopyToAsync(ms);

        var request = new DriveUploadRequest
        {
            WebPath = model.WebPath.Trim(),
            Path = model.Path.Trim(),
            FileName = string.IsNullOrWhiteSpace(model.FileName) ? model.File.FileName : model.FileName.Trim(),
            ExtraParams = string.IsNullOrWhiteSpace(model.ExtraParams) ? null : model.ExtraParams.Trim(),
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
}
