using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;

namespace PDFMergeService.Infrastructure.Services;

public class PdfMergeService : IPdfMergeService
{
    private readonly ILogger<PdfMergeService> _logger;

    public PdfMergeService(ILogger<PdfMergeService> logger)
    {
        _logger = logger;
    }

    public Task<byte[]> MergeAsync(PdfMergeRequest request)
    {
        if (request.Files.Count == 0)
            throw new ArgumentException("Birleştirilecek dosya bulunamadı.");

        var orderedFiles = request.Files.OrderBy(f => f.Order).ToList();

        using var outputDoc = new PdfDocument();

        foreach (var fileInfo in orderedFiles)
        {
            if (!File.Exists(fileInfo.TempFilePath))
            {
                _logger.LogWarning("Temp dosyası bulunamadı: {Path}", fileInfo.TempFilePath);
                continue;
            }

            using var sourceDoc = PdfReader.Open(fileInfo.TempFilePath, PdfDocumentOpenMode.Import);
            for (int i = 0; i < sourceDoc.PageCount; i++)
            {
                outputDoc.AddPage(sourceDoc.Pages[i]);
            }

            _logger.LogDebug("{FileName} eklendi ({Pages} sayfa)", fileInfo.FileName, sourceDoc.PageCount);
        }

        using var ms = new MemoryStream();
        outputDoc.Save(ms);
        return Task.FromResult(ms.ToArray());
    }
}
