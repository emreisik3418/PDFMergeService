namespace PDFMergeService.Web.ViewModels.FolderMerge;

public class FolderInfoViewModel
{
    public string FolderName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public List<string> PdfFiles { get; set; } = new();
    public int PdfCount => PdfFiles.Count;

    // Kullanıcının birleştirme öncesi belirlediği çıktı adı (uzantısız); boşsa klasör adı.
    public string? OutputFileName { get; set; }

    // ZIP içindeki klasör ("Akdeniz"); boşsa dosya ZIP'in köküne konur.
    public string? OutputDirectory { get; set; }
}
