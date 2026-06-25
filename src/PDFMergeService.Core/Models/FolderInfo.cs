namespace PDFMergeService.Core.Models;

public class FolderInfo
{
    public string FolderName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public List<string> PdfFiles { get; set; } = new();
    public int PdfCount => PdfFiles.Count;
}
