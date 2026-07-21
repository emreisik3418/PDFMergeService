namespace PDFMergeService.Web.ViewModels.DriveTransfer;

public class DriveTransferBulkItemViewModel
{
    public IFormFile? File { get; set; }
    public string Path { get; set; } = string.Empty;
    public bool IsMergedVersion { get; set; }
}

public class DriveTransferBulkUploadViewModel
{
    public List<DriveTransferBulkItemViewModel> Items { get; set; } = new();
}
