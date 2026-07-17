namespace PDFMergeService.Web.ViewModels.DriveTransfer;

public class DriveTransferUploadViewModel
{
    public IFormFile? File { get; set; }
    public string WebPath { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? ExtraParams { get; set; }
}
