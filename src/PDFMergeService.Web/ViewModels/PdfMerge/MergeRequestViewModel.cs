using PDFMergeService.Web.ViewModels.Shared;

namespace PDFMergeService.Web.ViewModels.PdfMerge;

public class MergeRequestViewModel
{
    public List<UploadedFileViewModel> Files { get; set; } = new();
    public FooterSettingsViewModel Footer { get; set; } = new();
}
