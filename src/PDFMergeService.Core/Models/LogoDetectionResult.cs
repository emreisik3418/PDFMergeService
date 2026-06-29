namespace PDFMergeService.Core.Models;

public class LogoDetectionResult
{
    public List<int> LogoPages { get; set; } = new();
    public int TotalPages { get; set; }
}
