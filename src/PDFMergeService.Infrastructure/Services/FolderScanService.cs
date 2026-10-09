using Microsoft.Extensions.Logging;
using PDFMergeService.Core.Interfaces;
using PDFMergeService.Core.Models;

namespace PDFMergeService.Infrastructure.Services;

public class FolderScanService : IFolderScanService
{
    // Ana klasörün altında inilecek en fazla seviye. Bireysel ve Karma Şubeler yapısı 3 seviyedir:
    // Bölge › Şube › Dönem (ör. Akdeniz › Akdeniz Bulvarı Antalya › 2026 - Ağustos).
    private const int MaxDepth = 4;

    private readonly ILogger<FolderScanService> _logger;

    public FolderScanService(ILogger<FolderScanService> logger)
    {
        _logger = logger;
    }

    // PDF içeren her alt klasör (hangi seviyede olursa olsun) ayrı bir birleştirme birimidir.
    // Ana klasörün kendi PDF'leri yok sayılır. Klasörler ve dosyalar doğal sıralıdır (1, 2, 10).
    public Task<List<FolderInfo>> ScanAsync(string rootPath)
    {
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException($"Klasör bulunamadı: {rootPath}");

        var result = new List<FolderInfo>();
        ScanChildren(rootPath, relativePath: string.Empty, depth: 1, result);
        return Task.FromResult(result);
    }

    private void ScanChildren(string parentPath, string relativePath, int depth, List<FolderInfo> result)
    {
        if (depth > MaxDepth)
            return;

        var subDirs = Directory.GetDirectories(parentPath)
            .OrderBy(d => Path.GetFileName(d), NaturalStringComparer.Instance)
            .ToList();

        foreach (var dir in subDirs)
        {
            var folderName = Path.GetFileName(dir);
            var childRelativePath = relativePath.Length == 0 ? folderName : $"{relativePath}/{folderName}";

            var pdfs = Directory.GetFiles(dir, "*.pdf", SearchOption.TopDirectoryOnly)
                .OrderBy(f => Path.GetFileName(f), NaturalStringComparer.Instance)
                .ToList();

            if (pdfs.Count > 0)
            {
                result.Add(new FolderInfo
                {
                    FolderName = folderName,
                    FolderPath = dir,
                    RelativePath = childRelativePath,
                    PdfFiles = pdfs
                });
            }
            else
            {
                _logger.LogDebug("PDF bulunamadı: {Dir}", dir);
            }

            ScanChildren(dir, childRelativePath, depth + 1, result);
        }
    }
}
