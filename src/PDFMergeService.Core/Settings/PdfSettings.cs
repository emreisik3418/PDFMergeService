namespace PDFMergeService.Core.Settings;

public class PdfSettings
{
    public string DefaultLogoPath { get; set; } = "wwwroot/assets/logo/company-logo.png";
    public int MaxFileSizeMB { get; set; } = 50;
    public int MaxFileCount { get; set; } = 20;
    public string TempFolder { get; set; } = "wwwroot/temp";
}
