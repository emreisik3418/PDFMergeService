using PDFMergeService.Core.Settings;

namespace PDFMergeService.Web.ViewModels.DriveTransfer;

public class DriveTransferIndexViewModel
{
    public List<WebPathOption> WebPathOptions { get; set; } = new();
    public List<BulkUploadRule> BulkUploadRules { get; set; } = new();
    public List<BulkPathOverride> BulkPathOverrides { get; set; } = new();
}
