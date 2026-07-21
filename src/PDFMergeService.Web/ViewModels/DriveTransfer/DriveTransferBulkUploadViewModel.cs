namespace PDFMergeService.Web.ViewModels.DriveTransfer;

public class DriveTransferBulkItemViewModel
{
    public IFormFile? File { get; set; }
    public string Path { get; set; } = string.Empty;
}

public class DriveTransferBulkUploadViewModel
{
    public List<DriveTransferBulkItemViewModel> Items { get; set; } = new();
    public string WebPath { get; set; } = string.Empty;
    public string? ExtraParams { get; set; }
    public bool IsMergedVersion { get; set; }
}
