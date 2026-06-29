namespace PDFMergeService.Web.ViewModels.PdfMerge;

public class DetectLogoRequestViewModel
{
    public List<UploadedFileViewModel> Files { get; set; } = new();
    public string? CustomLogoPath { get; set; }
}
