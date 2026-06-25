namespace PDFMergeService.Core.Models;

public class PdfFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string TempFilePath { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public long FileSize { get; set; }
    public int Order { get; set; }

    public string FileSizeFormatted => FileSize switch
    {
        < 1024 => $"{FileSize} B",
        < 1024 * 1024 => $"{FileSize / 1024.0:F1} KB",
        _ => $"{FileSize / (1024.0 * 1024):F1} MB"
    };
}
