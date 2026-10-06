using PDFMergeService.Core.Settings;

namespace PDFMergeService.Core.Models;

public class DrivePathConfig
{
    public List<WebPathOption> WebPathOptions { get; set; } = new();
    public List<WebPathOption> BulkRootPathOptions { get; set; } = new();
    public string BulkWebPath { get; set; } = string.Empty;

    // Toplu yüklemede dosya adından bölge/şube adını ayıklarken aranan ekler (ör. "BÖLGE MÜDÜRLÜĞÜ").
    public List<string> RegionSuffixes { get; set; } = new();
}
