namespace PDFMergeService.Web.ViewModels.PdfMerge;

public class UploadedFileViewModel
{
    public string FileName { get; set; } = string.Empty;
    public string TempFilePath { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public long FileSize { get; set; }
    public int Order { get; set; }
    public string FileSizeFormatted { get; set; } = string.Empty;
}
