namespace PDFMergeService.Web.ViewModels.FolderMerge;

public class FolderMergeTransferRequestViewModel : FolderMergeRequestViewModel
{
    public string WebPath { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? ExtraParams { get; set; }
}
