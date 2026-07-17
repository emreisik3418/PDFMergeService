namespace PDFMergeService.Core.Settings;

public class SharePointSettings
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
}
