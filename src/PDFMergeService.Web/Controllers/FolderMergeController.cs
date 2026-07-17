using System.IO.Compression;
using Microsoft.AspNetCore.Mvc;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Web.ViewModels.FolderMerge;
using PDFMergeService.Web.ViewModels.Shared;

namespace PDFMergeService.Web.Controllers;

public class FolderMergeController : Controller
{
    private readonly IFolderScanService _folderScanService;
    private readonly IPdfMergeService _pdfMergeService;
    private readonly IPdfFooterService _pdfFooterService;
    private readonly IPdfLogoDetectionService _logoDetectionService;
    private readonly IDriveTransferService _driveTransferService;
    private readonly ILogger<FolderMergeController> _logger;

    public FolderMergeController(
        IFolderScanService folderScanService,
        IPdfMergeService pdfMergeService,
        IPdfFooterService pdfFooterService,
        IPdfLogoDetectionService logoDetectionService,
        IDriveTransferService driveTransferService,
        ILogger<FolderMergeController> logger)
    {
        _folderScanService = folderScanService;
        _pdfMergeService = pdfMergeService;
        _pdfFooterService = pdfFooterService;
        _logoDetectionService = logoDetectionService;
        _driveTransferService = driveTransferService;
        _logger = logger;
    }

    [HttpGet("/folder-merge")]
    public IActionResult Index() => View();

    [HttpPost("/folder-merge/scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto?.RootPath))
            return BadRequest(new { error = "Klasör yolu boş olamaz." });

        var normalizedPath = dto.RootPath.Trim();

        if (normalizedPath.Contains(".."))
            return BadRequest(new { error = "Geçersiz klasör yolu." });

        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        bool isRemoteRequest = remoteIp != null && !System.Net.IPAddress.IsLoopback(remoteIp);
        bool isUncPath = normalizedPath.StartsWith(@"\\") || normalizedPath.StartsWith("//");

        if (isRemoteRequest && !isUncPath)
            return BadRequest(new { error = "Ağ üzerinden erişimde sunucu üzerindeki yerel yollar kullanılamaz. Lütfen UNC yolu kullanın. Örnek: \\\\SunucuAdı\\PaylaşımAdı\\Klasör" });

        try
        {
            var folders = await _folderScanService.ScanAsync(normalizedPath);

            var result = folders.Select(f => new FolderInfoViewModel
            {
                FolderName = f.FolderName,
                FolderPath = f.FolderPath,
                PdfFiles = f.PdfFiles.Select(Path.GetFileName).ToList()!
            }).ToList();

            return Ok(result);
        }
        catch (DirectoryNotFoundException)
        {
            var hint = isUncPath
                ? "Klasör bulunamadı. UNC yolunun erişilebilir ve doğru yazıldığından emin olun."
                : "Klasör bulunamadı. Yolu kontrol edin.";
            return BadRequest(new { error = hint });
        }
        catch (UnauthorizedAccessException)
        {
            return BadRequest(new { error = "Bu klasöre erişim izniniz yok. Ağ paylaşım izinlerini kontrol edin." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Klasör tarama hatası: {Path}", normalizedPath);
            return StatusCode(500, new { error = "Klasör tarama sırasında hata oluştu." });
        }
    }

    [HttpPost("/folder-merge/merge-all")]
    public async Task<IActionResult> MergeAll([FromBody] FolderMergeRequestViewModel model)
    {
        if (model.Folders == null || model.Folders.Count == 0)
            return BadRequest(new { error = "Birleştirilecek klasör bulunamadı." });

        var footer = MapFooterSettings(model.Footer);
        var today = DateTime.Now.ToString("yyyyMMdd");

        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var folder in model.Folders)
            {
                if (folder.PdfFiles == null || folder.PdfFiles.Count == 0) continue;

                var request = new PdfMergeRequest
                {
                    Files = folder.PdfFiles.Select((fileName, i) => new PdfFileInfo
                    {
                        FileName = fileName,
                        TempFilePath = Path.Combine(folder.FolderPath, fileName),
                        Order = i
                    }).ToList(),
                    Footer = footer
                };

                try
                {
                    byte[] merged = await _pdfMergeService.MergeAsync(request);
                    byte[] final = await _pdfFooterService.ApplyFooterAsync(merged, footer);

                    var entryName = $"{SanitizeFileName(folder.FolderName)}_{today}.pdf";
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    await entryStream.WriteAsync(final);

                    _logger.LogInformation("Birleştirildi: {Folder} ({Count} PDF)", folder.FolderName, folder.PdfFiles.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Birleştirme hatası: {Folder}", folder.FolderName);
                }
            }
        }

        zipStream.Position = 0;
        var zipName = $"TopluBirlestirme_{today}.zip";
        return File(zipStream.ToArray(), "application/zip", zipName);
    }

    [HttpPost("/folder-merge/transfer")]
    public async Task<IActionResult> Transfer([FromBody] FolderMergeTransferRequestViewModel model)
    {
        if (model.Folders == null || model.Folders.Count == 0)
            return BadRequest(new { error = "Aktarılacak klasör bulunamadı." });

        if (string.IsNullOrWhiteSpace(model.WebPath) || string.IsNullOrWhiteSpace(model.Path))
            return BadRequest(new { error = "Hedef yol (site alt yolu / klasör yolu) boş olamaz." });

        var footer = MapFooterSettings(model.Footer);
        var today = DateTime.Now.ToString("yyyyMMdd");
        var results = new List<object>();

        foreach (var folder in model.Folders)
        {
            if (folder.PdfFiles == null || folder.PdfFiles.Count == 0) continue;

            var fileName = $"{SanitizeFileName(folder.FolderName)}_{today}.pdf";

            var request = new PdfMergeRequest
            {
                Files = folder.PdfFiles.Select((f, i) => new PdfFileInfo
                {
                    FileName = f,
                    TempFilePath = Path.Combine(folder.FolderPath, f),
                    Order = i
                }).ToList(),
                Footer = footer
            };

            try
            {
                byte[] merged = await _pdfMergeService.MergeAsync(request);
                byte[] final = await _pdfFooterService.ApplyFooterAsync(merged, footer);

                var uploadResult = await _driveTransferService.UploadAsync(new DriveUploadRequest
                {
                    WebPath = model.WebPath.Trim(),
                    Path = model.Path.Trim(),
                    FileName = fileName,
                    ExtraParams = string.IsNullOrWhiteSpace(model.ExtraParams) ? null : model.ExtraParams.Trim(),
                    FileBytes = final
                });

                results.Add(new { folder = folder.FolderName, fileName, success = uploadResult.Success, message = uploadResult.Message });

                _logger.LogInformation("SharePoint aktarımı: {Folder} -> {Success}", folder.FolderName, uploadResult.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SharePoint aktarım hatası: {Folder}", folder.FolderName);
                results.Add(new { folder = folder.FolderName, fileName, success = false, message = "Beklenmeyen hata oluştu." });
            }
        }

        return Ok(new { results });
    }

    [HttpPost("/folder-merge/detect-logo")]
    public async Task<IActionResult> DetectLogo([FromBody] DetectLogoInFoldersViewModel model)
    {
        if (model.Folders == null || model.Folders.Count == 0)
            return Ok(new { logoPages = Array.Empty<int>(), checkedFiles = 0 });

        var allPages = new HashSet<int>();
        int checkedFiles = 0;

        foreach (var folder in model.Folders)
        {
            if (folder.PdfFiles == null) continue;

            int cumulative = 0;
            foreach (var fileName in folder.PdfFiles)
            {
                var filePath = Path.Combine(folder.FolderPath, fileName);
                if (!System.IO.File.Exists(filePath)) continue;

                var result = await _logoDetectionService.DetectLogoPagesAsync(filePath);
                foreach (var p in result.LogoPages)
                    allPages.Add(p + cumulative);

                cumulative += result.TotalPages;
                checkedFiles++;
            }
        }

        return Ok(new
        {
            logoPages = allPages.OrderBy(p => p).ToList(),
            checkedFiles
        });
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

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}

public class ScanRequestDto
{
    public string RootPath { get; set; } = string.Empty;
}
