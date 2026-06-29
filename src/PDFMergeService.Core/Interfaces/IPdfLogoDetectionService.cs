namespace PDFMergeService.Core.Interfaces;

public interface IPdfLogoDetectionService
{
    Task<Core.Models.LogoDetectionResult> DetectLogoPagesAsync(string pdfPath, string? customLogoPath = null);
}
