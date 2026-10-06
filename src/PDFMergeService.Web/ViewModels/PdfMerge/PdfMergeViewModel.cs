using PDFMergeService.Web.ViewModels.Shared;

namespace PDFMergeService.Web.ViewModels.PdfMerge;

public class PdfMergeViewModel
{
    public FooterSettingsViewModel Footer { get; set; } = new();
    public int MaxFileCount { get; set; }
    public int MaxFileSizeMB { get; set; }
}
