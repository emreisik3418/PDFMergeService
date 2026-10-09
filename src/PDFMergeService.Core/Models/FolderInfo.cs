namespace PDFMergeService.Core.Models;

public class FolderInfo
{
    public string FolderName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;

    // Taranan ana klasöre göre yol ("Akdeniz/Akdeniz Bulvarı Antalya/2026 - Ağustos"); tek seviyede klasör adı.
    public string RelativePath { get; set; } = string.Empty;
    public List<string> PdfFiles { get; set; } = new();
    public int PdfCount => PdfFiles.Count;
}
