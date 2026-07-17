namespace PDFMergeService.Core.Settings;

public class SharePointSettings
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public List<WebPathOption> WebPathOptions { get; set; } = new();
}

public class WebPathOption
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
