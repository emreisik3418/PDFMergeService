using Microsoft.Extensions.Logging;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;

namespace PDFMergeService.Infrastructure.Services;

public class FolderScanService : IFolderScanService
{
    private readonly ILogger<FolderScanService> _logger;

    public FolderScanService(ILogger<FolderScanService> logger)
    {
        _logger = logger;
    }

    public Task<List<FolderInfo>> ScanAsync(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Klasör bulunamadı: {rootPath}");

        var subDirs = Directory.GetDirectories(rootPath)
            .OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new List<FolderInfo>();

        foreach (var dir in subDirs)
        {
            var pdfs = Directory.GetFiles(dir, "*.pdf", SearchOption.TopDirectoryOnly)
                .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (pdfs.Count == 0)
            {
                _logger.LogDebug("PDF bulunamadı, atlandı: {Dir}", dir);
                continue;
            }

            result.Add(new FolderInfo
            {
                FolderName = Path.GetFileName(dir),
                FolderPath = dir,
                PdfFiles = pdfs
            });
        }

        return Task.FromResult(result);
    }
}
