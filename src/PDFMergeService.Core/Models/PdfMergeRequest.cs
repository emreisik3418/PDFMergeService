namespace PDFMergeService.Core.Models;

public class PdfMergeRequest
{
    public List<PdfFileInfo> Files { get; set; } = new();
    public FooterSettings Footer { get; set; } = new();
}
