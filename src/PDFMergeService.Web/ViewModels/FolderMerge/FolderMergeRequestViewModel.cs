using PDFMergeService.Web.ViewModels.Shared;

namespace PDFMergeService.Web.ViewModels.FolderMerge;

public class FolderMergeRequestViewModel
{
    public List<FolderInfoViewModel> Folders { get; set; } = new();
    public FooterSettingsViewModel Footer { get; set; } = new();
}
