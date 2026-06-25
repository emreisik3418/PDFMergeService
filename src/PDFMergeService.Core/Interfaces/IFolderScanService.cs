using PDFMergeService.Core.Models;

namespace PDFMergeService.Core.Interfaces;

public interface IFolderScanService
{
    Task<List<FolderInfo>> ScanAsync(string rootPath);
}
