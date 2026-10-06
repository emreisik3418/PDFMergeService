namespace PDFMergeService.Core.Settings;

public class PdfSettings
{
    public string DefaultLogoPath { get; set; } = "wwwroot/assets/logo/company-logo.png";
    public int MaxFileSizeMB { get; set; } = 50;
    // MaxFileSizeMB / MaxFileCount yalnızca ilk değerdir; sonrasında LimitsFilePath'teki dosyadan okunur.
    public int MaxFileCount { get; set; } = 20;
    public string LimitsFilePath { get; set; } = "App_Data/pdf-limits.json";
    public string TempFolder { get; set; } = "wwwroot/temp";
}
