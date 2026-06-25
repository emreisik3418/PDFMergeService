using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfSharpCore.Pdf.IO;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;
using PDFMergeService.Core.Settings;

namespace PDFMergeService.Infrastructure.Services;

public class PdfInfoService : IPdfInfoService
{
    private readonly PdfSettings _settings;
    private readonly ILogger<PdfInfoService> _logger;

    public PdfInfoService(IOptions<PdfSettings> settings, ILogger<PdfInfoService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PdfFileInfo> GetFileInfoAsync(IFormFile file)
    {
        Directory.CreateDirectory(_settings.TempFolder);

        var tempFileName = $"{Guid.NewGuid()}.pdf";
        var tempPath = Path.Combine(_settings.TempFolder, tempFileName);

        await using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        int pageCount;
        try
        {
            using var doc = PdfReader.Open(tempPath, PdfDocumentOpenMode.InformationOnly);
            pageCount = doc.PageCount;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sayfa sayısı okunamadı: {FileName}", file.FileName);
            pageCount = 0;
        }

        return new PdfFileInfo
        {
            FileName = file.FileName,
            TempFilePath = tempPath,
            PageCount = pageCount,
            FileSize = file.Length
        };
    }
}
