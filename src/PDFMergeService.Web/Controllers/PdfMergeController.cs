using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Web.ViewModels.PdfMerge;
using PDFMergeService.Web.ViewModels.Shared;

namespace PDFMergeService.Web.Controllers;

public class PdfMergeController : Controller
{
    private readonly IFileValidationService _validationService;
    private readonly IPdfInfoService _pdfInfoService;
    private readonly IPdfMergeService _pdfMergeService;
    private readonly IPdfFooterService _pdfFooterService;
    private readonly IPdfLogoDetectionService _logoDetectionService;
    private readonly ILogger<PdfMergeController> _logger;

    public PdfMergeController(
        IFileValidationService validationService,
        IPdfInfoService pdfInfoService,
        IPdfMergeService pdfMergeService,
        IPdfFooterService pdfFooterService,
        IPdfLogoDetectionService logoDetectionService,
        ILogger<PdfMergeController> logger)
    {
        _validationService = validationService;
        _pdfInfoService = pdfInfoService;
        _pdfMergeService = pdfMergeService;
        _pdfFooterService = pdfFooterService;
        _logoDetectionService = logoDetectionService;
        _logger = logger;
    }

    [HttpGet("/")]
    public IActionResult Index()
    {
        return View(new PdfMergeViewModel());
    }

    [HttpPost("/upload")]
    public async Task<IActionResult> UploadFiles(IFormFileCollection files)
    {
        var validation = await _validationService.ValidateAsync(files);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors });

        var results = new List<UploadedFileViewModel>();

        for (int i = 0; i < files.Count; i++)
        {
            var fileInfo = await _pdfInfoService.GetFileInfoAsync(files[i]);
            results.Add(new UploadedFileViewModel
            {
                FileName = fileInfo.FileName,
                TempFilePath = fileInfo.TempFilePath,
                PageCount = fileInfo.PageCount,
                FileSize = fileInfo.FileSize,
                FileSizeFormatted = fileInfo.FileSizeFormatted,
                Order = i
            });
        }

        return Ok(results);
    }

    [HttpPost("/merge")]
    public async Task<IActionResult> Merge([FromBody] MergeRequestViewModel model)
    {
        if (model.Files == null || model.Files.Count == 0)
            return BadRequest(new { errors = new[] { "Birleştirilecek dosya bulunamadı." } });

        var request = new PdfMergeRequest
        {
            Files = model.Files.Select(f => new PdfFileInfo
            {
                FileName = f.FileName,
                TempFilePath = f.TempFilePath,
                PageCount = f.PageCount,
                FileSize = f.FileSize,
                Order = f.Order
            }).ToList(),
            Footer = MapFooterSettings(model.Footer)
        };

        byte[] mergedBytes = await _pdfMergeService.MergeAsync(request);
        byte[] finalBytes = await _pdfFooterService.ApplyFooterAsync(mergedBytes, request.Footer);

        CleanupTempFiles(request.Files);

        var fileName = $"merged_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        return File(finalBytes, "application/pdf", fileName);
    }

    [HttpPost("/detect-logo")]
    public async Task<IActionResult> DetectLogo([FromBody] DetectLogoRequestViewModel model)
    {
        if (model.Files == null || model.Files.Count == 0)
            return Ok(new { logoPages = Array.Empty<int>(), checkedFiles = 0 });

        var logoPages = new List<int>();
        int cumulativePage = 0;
        int checkedFiles = 0;

        foreach (var file in model.Files.OrderBy(f => f.Order))
        {
            if (!System.IO.File.Exists(file.TempFilePath))
            {
                cumulativePage += file.PageCount;
                continue;
            }

            var result = await _logoDetectionService.DetectLogoPagesAsync(file.TempFilePath, model.CustomLogoPath);
            logoPages.AddRange(result.LogoPages.Select(p => p + cumulativePage));
            cumulativePage += file.PageCount;
            checkedFiles++;
        }

        return Ok(new
        {
            logoPages = logoPages.Distinct().OrderBy(p => p).ToList(),
            checkedFiles
        });
    }

    [HttpPost("/upload-logo")]
    public async Task<IActionResult> UploadLogo(IFormFile logo)
    {
        if (logo == null || logo.Length == 0)
            return BadRequest(new { error = "Logo dosyası gerekli." });

        var ext = Path.GetExtension(logo.FileName).ToLowerInvariant();
        if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
            return BadRequest(new { error = "Sadece PNG veya JPEG logo kabul edilir." });

        var tempPath = Path.Combine("wwwroot/temp", $"logo_{Guid.NewGuid()}{ext}");
        Directory.CreateDirectory("wwwroot/temp");

        await using var stream = new FileStream(tempPath, FileMode.Create);
        await logo.CopyToAsync(stream);

        return Ok(new { logoPath = tempPath });
    }

    private static FooterSettings MapFooterSettings(FooterSettingsViewModel vm) => new()
    {
        PageNumberEnabled = vm.PageNumberEnabled,
        StartFromPage = vm.StartFromPage,
        PageNumberPosition = vm.PageNumberPosition,
        FontSize = vm.FontSize,
        FontColor = vm.FontColor,
        LogoEnabled = vm.LogoEnabled,
        CustomLogoPath = vm.CustomLogoPath,
        LogoPosition = vm.LogoPosition,
        LogoWidth = vm.LogoWidth,
        LogoHeight = vm.LogoHeight,
        MarginBottom = vm.MarginBottom,
        MarginHorizontal = vm.MarginHorizontal,
        LogoSkipPages = vm.LogoSkipPages
    };

    private void CleanupTempFiles(List<PdfFileInfo> files)
    {
        foreach (var file in files)
        {
            try { System.IO.File.Delete(file.TempFilePath); }
            catch (Exception ex) { _logger.LogWarning(ex, "Temp dosyası silinemedi: {Path}", file.TempFilePath); }
        }
    }
}
