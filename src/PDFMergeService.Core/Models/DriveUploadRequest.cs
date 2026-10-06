namespace PDFMergeService.Core.Models;

public class DriveUploadRequest
{
    // Yükleyen kullanıcının SharePoint id'si; null ise SharePointSettings:UserId gönderilir.
    public int? UserId { get; set; }
    public string WebPath { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string[] ExtraParams { get; set; } = Array.Empty<string>();
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
}
