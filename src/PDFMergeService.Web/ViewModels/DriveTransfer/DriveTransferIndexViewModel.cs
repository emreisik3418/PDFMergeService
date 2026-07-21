using PDFMergeService.Core.Settings;

namespace PDFMergeService.Web.ViewModels.DriveTransfer;

public class DriveTransferIndexViewModel
{
    public List<WebPathOption> WebPathOptions { get; set; } = new();
    public List<WebPathOption> BulkRootPathOptions { get; set; } = new();
}
