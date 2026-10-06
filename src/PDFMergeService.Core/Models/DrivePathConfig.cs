using PDFMergeService.Core.Settings;

namespace PDFMergeService.Core.Models;

public class DrivePathConfig
{
    public List<WebPathOption> WebPathOptions { get; set; } = new();
    public string BulkWebPath { get; set; } = string.Empty;

    // Toplu yüklemede dosya adına göre hedef klasörü belirleyen kurallar.
    public List<BulkUploadRule> BulkUploadRules { get; set; } = new();
}
