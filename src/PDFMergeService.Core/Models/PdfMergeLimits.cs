namespace PDFMergeService.Core.Models;

// PDF Birleştir sayfasındaki yükleme sınırları; PDF Ayarları sayfasından düzenlenir.
public class PdfMergeLimits
{
    public const int MinFileCount = 2;
    public const int MaxFileCountLimit = 1000;
    public const int MinFileSizeMB = 1;
    public const int MaxFileSizeMBLimit = 500;

    public int MaxFileCount { get; set; }
    public int MaxFileSizeMB { get; set; }
}
