namespace PDFMergeService.Core.Models;

public class DriveUploadRequest
{
    public string WebPath { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? ExtraParams { get; set; }
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
}
